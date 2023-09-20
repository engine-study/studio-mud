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

        private static int transactionCount = 0;
        private static int transactionCompleted = 0;

        public bool Verbose = true;

        void Awake() {
            Instance = this;
        }
        void OnDestroy() {
            Instance = null;
            transactionCount = 0;
            transactionCompleted = 0;
        }
    
        public static bool CanSendTx { get { if(transactionCompleted != transactionCount) Debug.LogError("Too many transactions");  return transactionCompleted == transactionCount; } }

        public static async UniTask<bool> Send<TFunction>(TxUpdate update, params object[] parameters) where TFunction : FunctionMessage, new() {
            if (!CanSendTx) { return false; }
            return await Send<TFunction>(new List<TxUpdate> { update }, parameters);
        }

        //send optimistic
        public static async UniTask<bool> Send<TFunction>(List<TxUpdate> updates, params object[] parameters) where TFunction : FunctionMessage, new() {
            if (updates == null || updates.Count == 0 || updates.GetType() != typeof(List<TxUpdate>)) { Debug.LogError("No optimistic updates, use SendDirect instead"); return false; }
            if (!CanSendTx) { return false; }

            UniTask<bool> tx = SendQueue<TFunction>(parameters);
            
            foreach (TxUpdate u in updates) { u.Apply(tx); }

            bool txSuccess = await tx;

            foreach (TxUpdate u in updates) { u.Complete(txSuccess); }

            return txSuccess;
        }
        

        //only lets one transation send at a time
        public static async UniTask<bool> SendQueue<TFunction>(params object[] parameters) where TFunction : FunctionMessage, new() {

            if (!CanSendTx) { return false; }

            transactionCount++;

            bool txSuccess = await SendDirect<TFunction>(parameters);

            transactionCompleted++;
            return txSuccess;
        }


        public static async UniTask<bool> SendUntilPasses<TFunction>(params object[] parameters) where TFunction : FunctionMessage, new() { return await SendUntilPasses<TFunction>(1500, 5, parameters); }
        public static async UniTask<bool> SendUntilPasses<TFunction>(int millisecondDelay = 1500, int attempts = 5, params object[] parameters) where TFunction : FunctionMessage, new() {

            int timeout = attempts;
            bool txSuccess = false;

            while(txSuccess == false && timeout > 0) {
                txSuccess = await SendDirect<TFunction>(parameters);
                if (!txSuccess) { attempts--; await UniTask.Delay(1500); }
            }

            return txSuccess;
        }


        //sends tx directly
        public static async UniTask<bool> SendDirect<TFunction>(params object[] parameters) where TFunction : FunctionMessage, new() {
            if(Instance.Verbose) Debug.Log("[Tx SENT] " + typeof(TFunction).Name);
            bool txSuccess = await NetworkManager.Instance.worldSend.TxExecute<TFunction>(parameters);
            if(Instance.Verbose) Debug.Log("[Tx " + (txSuccess ? "CONFIRM" : "REVERT") + "] " + typeof(TFunction).Name);
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
            TxUpdate update = new TxUpdate(component, UpdateType.DeleteRecord, null);
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
        public UpdateInfo(UpdateInfo copyInfo) {
            updateType = copyInfo.UpdateType;
            source = copyInfo.Source;
        }

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
        public static Action<TxUpdate> OnUpdated;

        public UpdateInfo Info {get{return info;}}
        [SerializeField] private UpdateInfo info;
        [SerializeField] private MUDComponent component;
        [SerializeField] private IMudTable optimistic;
        [SerializeField] private UniTask<bool> tx;
        
        public TxUpdate(MUDComponent c, UpdateType newType, params object[] tableParameters) {
            
            //to be safe don't let optimistic updates override optimistic components
            if (c.IsOptimistic) { Debug.LogError(c.gameObject.name + ": Already optimistic.", c); return; }

            component = c;

            //derive table from component
            Type tableType = component.TableType;
            info = new UpdateInfo(newType, UpdateSource.Optimistic);

            //create an optimistic table
            if(info.UpdateType == UpdateType.SetRecord || info.UpdateType == UpdateType.SetField) {
                optimistic = (IMudTable)System.Activator.CreateInstance(tableType);
                optimistic.SetValues(tableParameters);
            } else {
                if (component.OnchainTable == null) { Debug.LogError(component.gameObject.name + ": No onchain table", c); }
                optimistic = component.OnchainTable;
            }

        }

        public void Apply(UniTask<bool> newTX) { 
            
            tx = newTX;
            
            component.SetOptimistic(this);

            if(info.UpdateType == UpdateType.SetRecord || info.UpdateType == UpdateType.SetField) {
                component.DoUpdate(optimistic, info);
            } else if(info.UpdateType == UpdateType.DeleteRecord) {
                component.DoUpdate(component.OnchainTable, info);
            } else {
                Debug.LogError("?");
            }

            OnUpdated?.Invoke(this);
        }

        public void Complete(bool success) {

            //not great but components cant release themselves from optimistic updates
            //but we also want components to have gotten their update before we send new updates
            component.SetOptimistic(null);
            
            if(success) {

            } else {
                Revert();
            }

            
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

            OnUpdated?.Invoke(this);

        }



    }
}
