using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using mud.Client;
using NetworkManager = mud.Unity.NetworkManager;

namespace mud.Client {


    public abstract class MUDComponent : MonoBehaviour {

        public MUDEntity Entity { get { return entity; } }
        public bool Loaded { get { return loaded; } }
        public bool HasInit { get { return hasInit; } }
        public IMudTable ActiveTable { get { return activeTable; } }
        public List<MUDComponent> RequiredComponents { get { return requiredComponents; } }
        public Action OnLoaded, OnUpdated;
        public Action<UpdateEvent> OnUpdatedDetails;
        public Action<MUDComponent, UpdateEvent> OnUpdatedFull;
        public TableManager TableManager { get { return tableManager; } }
        // public Type TableType {get{return tableType.Table;}}
        // public Type TableUpdateType {get{return tableType.TableUpdate;}}        
        public Type TableType {get{if(internalRef == null) LoadAssembly(); return internalRef.TableType();}}
        public Type TableTypeUpdate {get{if(internalRef == null) LoadAssembly(); return internalRef.TableUpdateType();}}

        [Header("Settings")]
        [SerializeField] private MUDTableObject tableType;
        [SerializeField] private List<MUDComponent> requiredComponents;


        [Header("Debug")]
        [SerializeField] private MUDEntity entity;
        [SerializeField] private TableManager tableManager;
        [SerializeField] private IMudTable activeTable;
        [SerializeField] private bool hasInit = false;
        [SerializeField] private bool loaded = false;
        private IMudTable onchainTable;
        private IMudTable optimisticTable;
        private IMudTable internalRef;

        protected virtual void Awake() {}
        protected virtual void Start() {}
        protected virtual void OnEnable() {}
        protected virtual void OnDisable() {}

        public MUDComponent() {}

        public virtual void Init(MUDEntity ourEntity, TableManager ourTable) {

            Debug.Assert(tableType != null, gameObject.name + ": no table reference.", this);

            entity = ourEntity;
            tableManager = ourTable;

            tableManager.RegisterComponent(true, this);

            LoadComponents();

            hasInit = true;
        }

        async UniTaskVoid LoadComponents() {

            //always delay a frame so that RequiredComponents has been fully added to by any other scripts on Start and Awake ex. check ComponentSync
            //chop it up so that not everything loads at the same frame
            await UniTask.Delay(UnityEngine.Random.Range(100,200));
        
            for (int i = 0; i < requiredComponents.Count; i++) {
                Debug.Log("Loading " + i + " " + requiredComponents[i].GetType(), this);
                MUDComponent c = await entity.GetMUDComponentAsync(requiredComponents[i]);
                if(c == null) Debug.LogError("Could not load " + requiredComponents[i].GetType(), this);
            }

            loaded = true;
            OnLoaded?.Invoke();

        }

        protected virtual void OnDestroy() {
            if (hasInit) {
                InitDestroy();
            }
        }

        protected virtual void InitDestroy() {

        }

        public virtual void Cleanup() {
            tableManager.RegisterComponent(false, this);
        }

        public void DoUpdate(mud.Client.IMudTable table, UpdateEvent eventType) {
            //update our internal table
            IngestUpdate(table, eventType);

            //use internal table to update component
            UpdateComponent(activeTable, eventType);

            OnUpdated?.Invoke();
            OnUpdatedDetails?.Invoke(eventType);
            OnUpdatedFull?.Invoke(this, eventType);
        }

        protected virtual void IngestUpdate(mud.Client.IMudTable table, UpdateEvent eventType) {

            //set our onchain table
            if (eventType == UpdateEvent.Optimistic) {
                optimisticTable = table;
            } else if (eventType != UpdateEvent.Revert) {
                onchainTable = table;
            }

            if (eventType == UpdateEvent.Revert) {
                Debug.Assert(onchainTable != null, "Reverting " + gameObject.name + " onchain update", this);
                optimisticTable = null;
                activeTable = onchainTable;
            } else if(eventType == UpdateEvent.Delete) {
                //don't do anything on the event of a deletion, the table will be null, leave the activeTable to the last value 
            } else {
                activeTable = table;
            }

        }

        protected abstract void UpdateComponent(mud.Client.IMudTable table, UpdateEvent eventType);

        void LoadAssembly() {
            //find the mud namespace
            Debug.Assert(tableType != null, gameObject.name + ": no table reference.", this);
            string namespaceName = tableType.TableName.Substring(0, tableType.TableName.IndexOf("."));
            // Debug.Log("Namespace: " + namespaceName);
            System.Reflection.Assembly a = System.Reflection.Assembly.Load(namespaceName);
            Type t = a.GetType(tableType.TableName);
            internalRef = (IMudTable)System.Activator.CreateInstance(t);
        }

    }
}