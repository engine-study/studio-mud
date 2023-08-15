using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using mud.Client;
using NetworkManager = mud.Unity.NetworkManager;

namespace mud.Client {

    public enum UpdateSource {None, Onchain, Optimistic, Revert, Override}
    public abstract class MUDComponent : MonoBehaviour {

        public MUDEntity Entity { get { return entity; } }
        public bool Loaded { get { return loaded; } }
        public bool HasInit { get { return hasInit; } }
        public IMudTable ActiveTable { get { return activeTable; } }
        public IMudTable OnchainTable { get { return onchainTable; } }
        public UpdateInfo NetworkInfo {get{return networkInfo;}}
        public UpdateInfo UpdateInfo {get{return updateInfo;}}
        public UpdateSource UpdateSource { get { return updateInfo.UpdateSource; } }
        public UpdateType UpdateType { get { return updateInfo.UpdateType; } }
        public List<MUDComponent> RequiredComponents { get { return requiredComponents; } }
        public Action OnInit, OnLoaded, OnPostInit, OnUpdated;
        public Action<MUDComponent, UpdateInfo> OnUpdatedInfo;
        public TableManager TableManager { get { return tableManager; } }


        //all this junk is because Unity packages cant access the namespaces inside the UNity project
        //unless we were to manually add the DefaultNamespace to the UniMud package by name
        public IMudTable TableReference { get { return GetTable(); }}
        public string TableName { get { return GetTable().TableType().Name; }}
        public Type TableType { get { return GetTable().TableType(); }}
        public Type TableTypeUpdate { get { return GetTable().TableUpdateType(); }}
        // public Type TableType { get { if (internalRef == null) LoadAssembly(); return internalRef.TableType(); } }
        // public Type TableTypeUpdate { get { if (internalRef == null) LoadAssembly(); return internalRef.TableUpdateType(); } }

        [Header("Settings")]
        [SerializeField] private List<MUDComponent> requiredComponents;
        [NonSerialized] private MUDTableObject tableType;


        [Header("Debug")]
        [SerializeField] private MUDEntity entity;
        [SerializeField] private TableManager tableManager;
        [SerializeField] private UpdateInfo updateInfo, networkInfo;
        [SerializeField] private IMudTable activeTable;
        [SerializeField] private bool hasInit = false;
        [SerializeField] private bool loaded = false;
        private IMudTable onchainTable;
        private IMudTable overrideTable;
        private IMudTable optimisticTable;
        private IMudTable internalRef;

        protected virtual void Awake() { 
            hasInit = false;
            updateInfo = new UpdateInfo(UpdateType.SetRecord, UpdateSource.None);
            networkInfo = new UpdateInfo(UpdateType.SetRecord, UpdateSource.None);
        }
        protected virtual void Start() { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }

        public MUDComponent() { }

        public async void DoInit(MUDEntity ourEntity, TableManager ourTable) {

            //set up our entity and table hooks
            Init(ourEntity, ourTable);
            hasInit = true;
            OnInit?.Invoke();

            //get our required components and other references
            await DoLoad();
        }

        protected virtual void Init(MUDEntity ourEntity, TableManager ourTable) {

            Debug.Assert(hasInit == false, "Double init", this);
            // Debug.Assert(tableType != null, gameObject.name + ": no table reference.", this);

            entity = ourEntity;
            tableManager = ourTable;

            tableManager.RegisterComponent(true, this);

        }

        async UniTask DoLoad() {

            gameObject.SetActive(false);

            //always delay a frame so that RequiredComponents has been fully added to by any other scripts on Start and Awake ex. check ComponentSync
            //chop it up so that not everything loads at the same frame
            await UniTask.Delay(UnityEngine.Random.Range(100, 200));

            if(requiredComponents.Count > 0) {

                ScanComponents(null);
                if(!loaded) {
                    Entity.OnComponentAdded += ScanComponents;
                }
            } else {
                FinishLoad();
            }
        }

        void FinishLoad() {

            gameObject.SetActive(true);

            loaded = true;
            OnLoaded?.Invoke();

            PostInit();
            OnPostInit?.Invoke();
        }

        protected virtual void PostInit() {

        }

        void ScanComponents(MUDComponent newComponent) {

            int components = 0;
            foreach(MUDComponent c in Entity.Components) {
                if(requiredComponents.Contains( ComponentDictionary.FindPrefab(c) )) {
                    components++;
                }
            }

            if(components == requiredComponents.Count) {
                Entity.OnComponentAdded -= ScanComponents;
                FinishLoad();
            }
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
            tableManager.RegisterComponent(false, this);
            Entity.OnComponentAdded -= ScanComponents;
        }

        public void DoUpdate(mud.Client.IMudTable table, UpdateInfo newInfo) {

            if(table == null) {
                Debug.LogError("No table", this);
                return;
            }
            
            //update our internal table
            IngestUpdate(table, newInfo);

            //use internal table to update component
            UpdateComponent(activeTable, newInfo);

            FinishUpdate();

            OnUpdated?.Invoke();
            OnUpdatedInfo?.Invoke(this, newInfo);
        }

        protected virtual void IngestUpdate(mud.Client.IMudTable table, UpdateInfo newInfo) {

            if (newInfo.UpdateSource == UpdateSource.Onchain) {
                //ONCHAIN update

                if (newInfo.UpdateType == UpdateType.DeleteRecord) {
                    //don't update activeTable in the event of a deletion, leave it to last onchain value
                    //in the case of a REVERT, then we can fall back to the last onchain value
                } else {
                    onchainTable = table;
                    activeTable = onchainTable;
                }

                networkInfo = newInfo;

            } else if (newInfo.UpdateSource == UpdateSource.Optimistic) {
                //OPTIMISTIC update
                optimisticTable = table;
                activeTable = optimisticTable;
            } else if (newInfo.UpdateSource == UpdateSource.Override) {
                //OVERRIde update
                overrideTable = table;
                activeTable = overrideTable;
            } else if (newInfo.UpdateSource == UpdateSource.Revert) {
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

        protected abstract IMudTable GetTable();
        protected abstract void UpdateComponent(mud.Client.IMudTable table, UpdateInfo newInfo);

        void FinishUpdate() {

            // if (NetworkState == UpdateType.DeleteRecord) {
            //     gameObject.SetActive(false);
            // } else if (gameObject.activeSelf == false) {
            //     gameObject.SetActive(true);
            // }

        }

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