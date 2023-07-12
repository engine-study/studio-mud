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

        public static async UniTask<bool> SendSimple<TFunction>(MUDComponent component, params object[] parameters) where TFunction : FunctionMessage, new()
        {
            return await NetworkManager.Instance.worldSend.TxExecute<TFunction>(parameters);
        }

        //optimistically update something
        public static async UniTask<bool> Send<TFunction>(MUDComponent component, List<TxUpdate> updates, params object[] parameters) where TFunction : FunctionMessage, new()
        {
            bool txSuccess = await NetworkManager.Instance.worldSend.TxExecute<TFunction>(parameters);

            if(txSuccess)  {
                Debug.Log("Success");
            } else {
                //if our transaction fails, force the player back to their position on the table
                Debug.Log("Reverting");
                foreach(TxUpdate u in updates) { u.Revert();}
            }

            return txSuccess;
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
            Type tableType = component.ComponentToTableType;

            //create an optimistic table
            optimistic = (IMudTable)System.Activator.CreateInstance(tableType);
            optimistic.SetValues(tableParameters);

            component.UpdateComponentManual(optimistic, UpdateEvent.Optimistic);
        }

        public void Revert() {
            // component.UpdateComponentManual(component.TableManager.GetTableValues(component), UpdateEvent.Revert);
            component.UpdateComponentManual(null, UpdateEvent.Revert);
        }

        public MUDComponent component;
        public IMudTable optimistic;


    }
}
