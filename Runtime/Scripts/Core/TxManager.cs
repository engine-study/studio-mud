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

        public static async UniTaskVoid SendSimple<TFunction>(MUDComponent component, params object[] parameters) where TFunction : FunctionMessage, new()
        {
            try { await NetworkManager.Instance.worldSend.TxExecute<TFunction>(parameters); }
            catch (System.Exception ex) { Debug.LogException(ex); }
        }

        //optimistically update something
        public static async UniTaskVoid Send<TFunction>(MUDComponent component, List<TxUpdate> updates, params object[] parameters) where TFunction : FunctionMessage, new()
        {
            try
            {
                //send an optimistic table update and then the actual transaction
                // foreach(TxUpdate u in updates) {}
                await NetworkManager.Instance.worldSend.TxExecute<TFunction>(parameters);
            }
            catch (System.Exception ex)
            {
                //if our transaction fails, force the player back to their position on the table
                Debug.LogException(ex);
                foreach(TxUpdate u in updates) { u.Revert();}
            }
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
            component.UpdateComponentManual(component.TableManager.GetTableValues(component), UpdateEvent.Revert);
        }

        public MUDComponent component;
        public IMudTable optimistic;


    }
}
