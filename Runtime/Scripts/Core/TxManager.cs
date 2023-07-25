using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using mud.Unity;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Contracts.CQS;
using Nethereum.Contracts;
using Cysharp.Threading.Tasks;

namespace mud.Client {

    public class TxManager : MonoBehaviour {

        public static System.Action<bool> OnTransaction;

        //send transaction but revert our optimistic updates if it goes wrong
        public static async UniTask<bool> Send<TFunction>(TxUpdate update, params object[] parameters) where TFunction : FunctionMessage, new() {
            return await Send<TFunction>(new List<TxUpdate> { update }, parameters);
        }

        public static async UniTask<bool> Send<TFunction>(List<TxUpdate> updates, params object[] parameters) where TFunction : FunctionMessage, new() {
            bool txSuccess = await Send<TFunction>(parameters);

            if (txSuccess) {
                Debug.Log("Success: " + typeof(TFunction).ToString());
            } else {
                //if our transaction fails, force the player back to their position on the table
                Debug.Log("Reverting: " + typeof(TFunction).ToString());
                foreach (TxUpdate u in updates) { u.Revert(); }
            }

            return txSuccess;
        }

        public static async UniTask<bool> Send<TFunction>(params object[] parameters) where TFunction : FunctionMessage, new() {
            bool txSuccess = await NetworkManager.Instance.worldSend.TxExecute<TFunction>(parameters);
            OnTransaction?.Invoke(txSuccess);
            return txSuccess;
        }

        //enables us to send transactions by inferring functionTyp through the parameter
        // public static async UniTask<bool> Send<TFunction>(TFunction functionType, params object[] parameters) where TFunction : FunctionMessage, new() {
        //     return await Send<TFunction>(parameters);
        // }

        public static TxUpdate MakeOptimisticInsert<T>(string entityKey, params object[] tableParameters) where T : MUDComponent {
            //make the component

            //create the entity if it doesn't exist
            MUDComponent c = TableManager.FindOrMakeComponent<T>(entityKey);
            TxUpdate update = new TxUpdate(c, UpdateType.SetRecord, tableParameters);
            return update;
        }

        public static TxUpdate MakeOptimistic(MUDComponent component, params object[] tableParameters) {
            //update the component
            TxUpdate update = new TxUpdate(component, UpdateType.SetField, tableParameters);
            return update;
        }

        public static TxUpdate MakeOptimisticDelete(MUDComponent component, params object[] tableParameters) {
            //update the component
            TxUpdate update = new TxUpdate(component, UpdateType.DeleteRecord, tableParameters);
            return update;
        }


    }

    [System.Serializable]
    public class TxUpdate {
        public TxUpdate(MUDComponent c, UpdateType newType, params object[] tableParameters) {
            component = c;

            //derive table from component
            Type tableType = component.TableType;
            info = new UpdateInfo(newType, UpdateSource.Optimistic);

            //create an optimistic table
            optimistic = (IMudTable)System.Activator.CreateInstance(tableType);
            optimistic.SetValues(tableParameters);

            component.DoUpdate(optimistic, info);
        }

        public void Revert() {

            info = new UpdateInfo(info.UpdateType, UpdateSource.Optimistic);
            component.DoUpdate(component.OnchainTable, info);
            
            if (info.UpdateType == UpdateType.SetRecord) {
                component.Destroy();
            } else if (info.UpdateType == UpdateType.SetField) {
                
            } else {
                
            }

        }

        [SerializeField] private UpdateInfo info;
        [SerializeField] private MUDComponent component;
        [SerializeField] private IMudTable optimistic;


    }
}
