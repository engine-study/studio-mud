using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using mud.Client;
using NetworkManager = mud.Unity.NetworkManager;

namespace mud.Client
{

    public enum SpawnSource{Load, InGame}
    public enum UpdateSource {None, Onchain, Optimistic, Revert, Override}

    public abstract class MUDComponent : MonoBehaviour {

        public MUDEntity Entity { get { return entity; } }
        public bool Loaded { get { return loaded; } }
        public bool HasInit { get { return hasInit; } }
        public bool IsOptimistic { get; private set; }
        public IMudTable ActiveTable { get { return activeTable; } }
        public IMudTable OnchainTable { get { return onchainTable; } }
        public SpawnInfo SpawnInfo {get{return spawnInfo;}}
        public UpdateInfo NetworkInfo {get{return networkInfo;}}
        public UpdateInfo UpdateInfo {get{return updateInfo;}}
        //TODO change this so that it checks the types, not the prefabs themselves
        public List<Type> RequiredTypes { get { return requiredTypes; } }
        public List<MUDComponent> RequiredPrefabs { get { return requiredComponents; } }
        public Action OnInit, OnLoaded, OnStart;
        public Action OnUpdated, OnInstantUpdate, OnRichUpdate, OnValueUpdated, OnCreated, OnDeleted;
        public Action<MUDComponent, UpdateInfo> OnUpdatedInfo;
        public TableManager Manager { get { return spawnInfo.Table; } }


        //all this junk is because Unity packages cant access the namespaces inside the UNity project
        //unless we were to manually add the DefaultNamespace to the UniMud package by name
        public IMudTable TableReference { get { return GetTable(); }}
        public string MUDTableName { get { return GetTable().TableType().Name; }}
        public Type MUDTableType { get { return GetTable().TableType(); }}

        [Header("Settings")]
        [SerializeField] private List<MUDComponent> requiredComponents;
        [SerializeField] private List<Type> requiredTypes;
        [NonSerialized] private MUDTableObject tableType;

        private IMudTable activeTable;

        [Header("Debug")]
        [SerializeField] private MUDEntity entity;
        [SerializeField] private bool hasInit = false;
        [SerializeField] private bool loaded = false;
        [SerializeField] private SpawnInfo spawnInfo;
        [SerializeField] private UpdateInfo updateInfo, networkInfo, lastInfo;
        
        //tables
        private IMudTable onchainTable;
        private IMudTable overrideTable;
        private IMudTable optimisticTable;
        private IMudTable internalRef;
        private TxUpdate optimisticUpdate;


        protected virtual void Awake() { }
        protected virtual void Start() { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }

        public async void DoInit(SpawnInfo spawnInfo) {

            //set up our entity and table hooks
            Init(spawnInfo);

            hasInit = true;
            OnInit?.Invoke();

            //get our required components and other references
            await DoLoad();
        }

        protected virtual void Init(SpawnInfo newSpawnInfo) {

            Debug.Assert(hasInit == false, "Double init", this);

            requiredTypes = new List<Type>();
            for(int i = 0; i < requiredComponents.Count; i++) {requiredTypes.Add(requiredComponents[i].GetType());}

            updateInfo = new UpdateInfo(UpdateType.SetRecord, UpdateSource.None);
            networkInfo = new UpdateInfo(UpdateType.SetRecord, UpdateSource.None);

            // Debug.Assert(tableType != null, gameObject.name + ": no table reference.", this);
            spawnInfo = newSpawnInfo;
            entity = spawnInfo.Entity;

            if(spawnInfo.Table) {spawnInfo.Table.RegisterComponent(true, this);}

        }

        async UniTask DoLoad() {

            gameObject.SetActive(false);

            //always delay a frame so that RequiredComponents has been fully added to by any other scripts on Start and Awake ex. check ComponentSync
            //chop it up so that not everything loads at the same frame
            await UniTask.Delay(200);

            if(HasLoadedAllComponents()) {
                FinishLoad();
            } else {
                Entity.OnComponentAdded += EntityComponentUpdate;
            }
        }

        void EntityComponentUpdate(MUDComponent newComponent) {
            if( HasLoadedAllComponents() ) { FinishLoad();}
        }


        void FinishLoad() {
            Debug.Assert(loaded == false, "Already loaded", this);
            Entity.OnComponentAdded -= EntityComponentUpdate;

            gameObject.SetActive(true);

            loaded = true;
            OnLoaded?.Invoke();

            PostInit();
            OnStart?.Invoke();
            if (Manager.Loaded) { OnCreated?.Invoke(); }
            
        }

        protected virtual void PostInit() {
            //think of this like the true "Awake" of the MUDComponent
            //at this point the Entity will have loaded all the RequiredComponents
            //it will be safe to get components using Entity.GetMUDComponent<ExampleComponent();
        }

        bool HasLoadedAllComponents() {

            int components = 0;
            foreach(MUDComponent c in Entity.Components) {
                if(requiredTypes.Contains( c.Manager.Prefab.GetType() )) {
                    components++;
                }
            }

            return components == requiredTypes.Count;

        }

        public virtual void Destroy() {
            GameObject.Destroy(gameObject);
        }

        protected virtual void OnDestroy() {
            if (hasInit) {
                InitDestroy();
            }
        }

        protected virtual void InitDestroy() {
            if(spawnInfo.Table) {spawnInfo.Table.RegisterComponent(false, this);}
            if(Entity) {Entity.OnComponentAdded -= EntityComponentUpdate;}
        }

        public void DoUpdate(mud.Client.IMudTable table, UpdateInfo newInfo) {

            if(table == null) {
                Debug.LogError(gameObject.name + ": null table", this);
                return;
            }

            //update our internal table
            IngestUpdate(table, newInfo);
            UpdateComponent(activeTable, newInfo);

            // if(lastTable == null || !lastTable.Equals(activeTable)) OnValueUpdated?.Invoke();
            OnUpdated?.Invoke();
            OnUpdatedInfo?.Invoke(this, newInfo);
            
            if (IsRichUpdate()) {
                UpdateComponentRich();
                OnRichUpdate?.Invoke();
            } else {
                UpdateComponentInstant();
                OnInstantUpdate?.Invoke();
            }

        }

        protected virtual bool IsRichUpdate()
        {
            //check if onchain update connects to the local optimistic update?
            bool wasNotOptimistic = lastInfo != null && lastInfo.Source != UpdateSource.Optimistic;
            //&& !IMudTable.Equals(lastTable, activeTable)
            return Loaded && UpdateInfo.Source != UpdateSource.Revert && wasNotOptimistic;
        } 

        protected virtual void IngestUpdate(mud.Client.IMudTable table, UpdateInfo newInfo) {

            //cache last info
            lastInfo = new UpdateInfo(updateInfo);

            if (newInfo.Source == UpdateSource.Onchain) {
                //ONCHAIN update
                if (newInfo.UpdateType == UpdateType.DeleteRecord) {
                    //don't update activeTable in the event of a deletion, leave it to last onchain value
                    //in the case of a REVERT, then we can fall back to the last onchain value
                } else {
                    onchainTable = table;
                    activeTable = onchainTable;
                }

                networkInfo = newInfo;

            } else if (newInfo.Source == UpdateSource.Optimistic) {
                //OPTIMISTIC update
                optimisticTable = table;
                activeTable = optimisticTable;
            } else if (newInfo.Source == UpdateSource.Override) {
                //OVERRIde update
                overrideTable = table;
                activeTable = overrideTable;
            } else if (newInfo.Source == UpdateSource.Revert) {
                //REVERT update (undo optimistic update)
                Debug.Assert(onchainTable != null, "Reverting " + gameObject.name + ", but no onchain update", this);

                optimisticTable = null;
                activeTable = onchainTable;
            }

            // if we are GOING INTO or ALREADY IN an override state, ignore the update
            // if (UpdateInfo.UpdateSource == UpdateSource.Override || newInfo.UpdateSource == UpdateSource.Override) {
            //     activeTable = overrideTable;
            // }

            updateInfo = newInfo;

        }

        public void SetOptimistic(TxUpdate newUpdate) { 
            if (optimisticUpdate != null && newUpdate != null) {
                Debug.LogError(gameObject.name + ": already optimistic", this); return; 
            }
            optimisticUpdate = newUpdate;
            IsOptimistic = optimisticUpdate != null;
        }

        protected abstract IMudTable GetTable();
        protected abstract void UpdateComponent(mud.Client.IMudTable table, UpdateInfo newInfo);
        protected virtual void UpdateComponentInstant() { }
        protected virtual void UpdateComponentRich() { }

        public void ToggleRequiredComponent(bool toggle, MUDComponent prefab) {

            if(Loaded) {
                Debug.LogError("Already loaded", this);
            }

            if(toggle) {
                if (!requiredComponents.Contains(prefab)) { 
                    requiredComponents.Add(prefab); 
                    requiredTypes.Add(prefab.GetType());
                }
            } else {
                requiredComponents.Remove(prefab);
                requiredTypes.Remove(prefab.GetType());
            }
        }

    }
}