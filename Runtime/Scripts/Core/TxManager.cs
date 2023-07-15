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

namespace mud.Client
{

    public class TxManager : MonoBehaviour
    {

        public static System.Action<bool> OnTransaction;
        public static async UniTask<bool> Send<TFunction>(params object[] parameters) where TFunction : FunctionMessage, new()
        {
            bool txSuccess = await NetworkManager.Instance.worldSend.TxExecute<TFunction>(parameters);
            CleanupTransaction(txSuccess);
            return txSuccess;
        }

        //optimistically update something
        public static async UniTask<bool> Send<TFunction>(List<TxUpdate> updates, params object[] parameters) where TFunction : FunctionMessage, new()
        {
            bool txSuccess = await NetworkManager.Instance.worldSend.TxExecute<TFunction>(parameters);

            if(txSuccess)  {
                Debug.Log("Success");
            } else {
                //if our transaction fails, force the player back to their position on the table
                Debug.Log("Reverting");
                foreach(TxUpdate u in updates) { u.Revert();}
            }

            CleanupTransaction(txSuccess);
            return txSuccess;
        }

        private static void CleanupTransaction(bool txSuccess) {
            OnTransaction?.Invoke(txSuccess);
        }

        public static TxUpdate MakeOptimistic(MUDComponent component, params object[] tableParameters) {
            //update the component
            TxUpdate update = new TxUpdate(component, tableParameters);
            return update;
        }

    }

    [System.Serializable]
    public struct TxUpdate
    {
        public TxUpdate(MUDComponent c, params object[] tableParameters)
        {
            component = c;

            //derive table from component
            Type tableType = component.TableType;

            //create an optimistic table
            optimistic = (IMudTable)System.Activator.CreateInstance(tableType);
            optimistic.SetValues(tableParameters);

            component.DoUpdate(optimistic, UpdateEvent.Optimistic);
        }

        public void Revert() {
            // component.DoUpdate(component.TableManager.GetTableValues(component), UpdateEvent.Revert);
            component.DoUpdate(null, UpdateEvent.Revert);
        }

        public MUDComponent component;
        public IMudTable optimistic;


    }
}
