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

        public static TxManager Instance;
        public static System.Action<bool> OnUpdate;
        public static System.Action<bool> OnTransaction;
        public static bool InProgress;
        //send transaction but revert our optimistic updates if it goes wrong
        private static List<int> transactions = new List<int>();
        private static int transactionCount = 0;
        private static int transactionCompleted = 0;

        public bool Verbose = true;

        void Awake() {
            Instance = this;
            transactions = new List<int>();
        }
        void OnDestroy() {
            Instance = null;
            transactions = null;
            transactionCount = 0;
            transactionCompleted = 0;
        }
        public static async UniTask<bool> Send<TFunction>(TxUpdate update, params object[] parameters) where TFunction : FunctionMessage, new() {
            return await Send<TFunction>(new List<TxUpdate> { update }, parameters);
        }

        public static async UniTask<bool> Send<TFunction>(List<TxUpdate> updates, params object[] parameters) where TFunction : FunctionMessage, new() {
            bool txSuccess = await Send<TFunction>(parameters);

            if(!txSuccess) {
                foreach (TxUpdate u in updates) { u.Revert(); }
            }

            return txSuccess;
        }
        
        public static async UniTask<bool> Send<TFunction>(params object[] parameters) where TFunction : FunctionMessage, new() {

            int txIndex = transactionCount;
            transactionCount++;

            while (transactionCompleted != txIndex) { await UniTask.Delay(200); }
            if(Instance.Verbose) Debug.Log("[Tx SENT] " + typeof(TFunction).Name);

            bool txSuccess = await SendDirect<TFunction>(parameters);

            if(Instance.Verbose) Debug.Log("[Tx " + (txSuccess ? "CONFIRM" : "REVERT") + "] " + typeof(TFunction).Name);
            transactionCompleted++;
            return txSuccess;
        }

        public static async UniTask<bool> SendDirect<TFunction>(params object[] parameters) where TFunction : FunctionMessage, new() {
            bool txSuccess = await NetworkManager.Instance.worldSend.TxExecute<TFunction>(parameters);
            OnTransaction?.Invoke(txSuccess);
            return txSuccess;
        }

        //enables us to send transactions by inferring functionTyp through the parameter
        // public static async UniTask<bool> Send<TFunction>(TFunction functionType, params object[] parameters) where TFunction : FunctionMessage, new() {
        //     return await Send<TFunction>(parameters);
        // }

        public static TxUpdate MakeOptimisticInsert<T>(string entityKey, params object[] tableParameters) where T : MUDComponent, new() {
            //make the component
            //create the entity if it doesn't exist
            MUDComponent c = MUDWorld.FindOrMakeComponent<T>(entityKey);
            TxUpdate update = new TxUpdate(c, UpdateType.SetRecord, tableParameters);
            return update;
        }

        public static TxUpdate MakeOptimistic(MUDComponent component, params object[] tableParameters) {
            //update the component
            TxUpdate update = new TxUpdate(component, UpdateType.SetField, tableParameters);
            return update;
        }

        public static TxUpdate MakeOptimisticDelete(MUDComponent component) {
            //update the component
            TxUpdate update = new TxUpdate(component, UpdateType.DeleteRecord);
            return update;
        }


    }

    [System.Serializable]
    public class SpawnInfo {
        public SpawnInfo(MUDEntity newEntity, SpawnSource newSpawnSource, TableManager newSpawnTable) {
            entity = newEntity;
            source = newSpawnSource;
            table = newSpawnTable;
        }
        public MUDEntity Entity {get{return entity;}}
        public SpawnSource Source {get{return source;}}
        public TableManager Table {get{return table;}}
        [SerializeField] MUDEntity entity;
        [SerializeField] SpawnSource source;
        [SerializeField] TableManager table;

    }

    [System.Serializable]
    public class UpdateInfo {
        public UpdateInfo(UpdateType newUpdateType, UpdateSource newSource) {
            updateType = newUpdateType;
            source = newSource;
        }

        public UpdateType UpdateType {get{return updateType;}}
        public UpdateSource Source {get{return source;}}
        [SerializeField] UpdateType updateType;
        [SerializeField] UpdateSource source;
    }

    [System.Serializable]
    public class TxUpdate {
        public static Action<TxUpdate> OnUpdate;

        public UpdateInfo Info {get{return info;}}
        [SerializeField] private UpdateInfo info;
        [SerializeField] private MUDComponent component;
        [SerializeField] private IMudTable optimistic;
        //TODO add TX status
        
        public TxUpdate(MUDComponent c, UpdateType newType, params object[] tableParameters) {
            component = c;

            //derive table from component
            Type tableType = component.TableType;
            info = new UpdateInfo(newType, UpdateSource.Optimistic);

            if(newType == UpdateType.SetRecord || newType == UpdateType.SetField) {

                //create an optimistic table
                optimistic = (IMudTable)System.Activator.CreateInstance(tableType);
                optimistic.SetValues(tableParameters);

                component.DoUpdate(optimistic, info);

            } else if(newType == UpdateType.DeleteRecord) {
                
                //delete table
                component.DoUpdate(component.OnchainTable, info);
                
            } else {
                Debug.LogError("?");
            }

            OnUpdate?.Invoke(this);
        }

        public void Revert() {

            info = new UpdateInfo(info.UpdateType, UpdateSource.Revert);
            
            if (info.UpdateType == UpdateType.SetRecord) {
                component.Destroy();
            } else if (info.UpdateType == UpdateType.SetField) {
                component.DoUpdate(component.OnchainTable, info);
            } else {
                component.DoUpdate(component.OnchainTable, info);
            }

            OnUpdate?.Invoke(this);

        }




    }
}
