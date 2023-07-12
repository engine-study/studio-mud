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

namespace mud.Client
{

    public class TransactionManager : MonoBehaviour
    {

        //optimistically update something
        public async void SendUpdateTransaction<TFunction, T>(MUDComponent component, params object[] functionParameters) where TFunction : FunctionMessage, new() where T : IMudTable, new() {
           
            try
            {
                //create an optimistic table
                T optimisticTable = new T();
                optimisticTable.SetValues(functionParameters);

                //update the component
                component.UpdateComponentManual(optimisticTable, UpdateEvent.Optimistic);

                //send the actual transaction
                await NetworkManager.Instance.worldSend.TxExecute<TFunction>(functionParameters);
            }
            catch (System.Exception ex)
            {
                //if our transaction fails, force the player back to their position on the table
                Debug.LogException(ex);

                var revertTable = TableDictionary.GetTableValue<T>(component);
                
                component.UpdateComponentManual(revertTable, UpdateEvent.Revert);

            }
        }

    
        // public async Task TxExecute<TFunction>(params object[] functionParameters)
        //     where TFunction : FunctionMessage, new()
        // {
        //     // await _semaphore.Wa

        // }


        // //optimistically spawn something
        // public async string SendInsertTransaction<TFunction>(params object[] functionParameters) where TFunction : FunctionMessage, new() {
        //     try
        //     {
        //         // IMudTable fakeTable = new IMudTable();
        //         // fakeTable.value = (ulong)(rockType + 1);
        //         // function moveFrom(int32 startX, int32 startY, int32 x, int32 y) public {
        //         // UpdateComponent(fakeTable, UpdateEvent.Optimistic);
        //         await NetworkManager.Instance.worldSend.TxExecute<TFunction>(functionParameters);
        //     }
        //     catch (System.Exception ex)
        //     {
        //         //if our transaction fails, force the player back to their position on the table
        //         Debug.LogException(ex);
        //         // IMudTable fakeTable = new IMudTable();
        //         // fakeTable.value = RockTable.GetTableValue(Entity.Key).value;
        //         // UpdateComponent(fakeTable, UpdateEvent.Revert);

        //     }
        // }

        // //optimistically delete something
        // public async void SendDeleteTransaction<TFunction>(params object[] functionParameters) where TFunction : FunctionMessage, new() {
        //        try
        //     {
        //         // IMudTable fakeTable = new IMudTable();
        //         // fakeTable.value = (ulong)(rockType + 1);
        //         // function moveFrom(int32 startX, int32 startY, int32 x, int32 y) public {
        //         // UpdateComponent(fakeTable, UpdateEvent.Optimistic);
        //         await NetworkManager.Instance.worldSend.TxExecute<TFunction>(functionParameters);
        //     }
        //     catch (System.Exception ex)
        //     {
        //         //if our transaction fails, force the player back to their position on the table
        //         Debug.LogException(ex);
        //         // IMudTable fakeTable = new IMudTable();
        //         // fakeTable.value = RockTable.GetTableValue(Entity.Key).value;
        //         // UpdateComponent(fakeTable, UpdateEvent.Revert);

        //     }
        // }
    }
}
