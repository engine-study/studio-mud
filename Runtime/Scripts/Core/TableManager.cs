using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Unity;
using mud.Client;
using NetworkManager = mud.Unity.NetworkManager;
using UniRx;
using ObservableExtensions = UniRx.ObservableExtensions;
using System.Threading.Tasks;

namespace mud.Client {


    public class TableManager : MonoBehaviour {

        public Action<MUDComponent> OnComponentSpawned;
        public bool Loaded {get{return hasInit;}}
        protected CompositeDisposable _disposers = new();
        protected mud.Unity.NetworkManager net; 
        public Action OnInit;
        public Action OnAdded, OnUpdated, OnDeleted;


        //dictionary of all entities        
        public Type ComponentType { get { return componentPrefab.TableType; } }
        public string ComponentName { get { return componentPrefab.TableName; } }

        //dictionary of all the components this specific table has
        public bool EntityHasComponent(string key) { return Components.ContainsKey(key); }
        public MUDComponent EntityToComponent(string key) { return Components[key]; }
        public Dictionary<string, MUDComponent> Components;
        public MUDComponent Prefab { get { return componentPrefab; } }

        [Header("Settings")]
        public MUDComponent componentPrefab;
        public bool subscribeInsert = true, subscribeUpdate = true, subscribeDelete = true;
        [Header("Behaviour")]
        public bool deletedRecordDestroysEntity = false;

        [Header("Debug")]
        public bool logTable = false;
        public List<MUDComponent> SpawnedComponents;

        private IDisposable subscribe;
        bool hasInit;
        bool firstUpdate = true;

        // public Dictionary<string, MUDComponent> Components;


        protected void Awake() {
            
            SpawnedComponents = new List<MUDComponent>();

        }

        protected void Start() {

            net = mud.Unity.NetworkManager.Instance;

            if(NetworkManager.NetworkInitialized) {
                DoInit(net);
            } else {
                net.OnNetworkInitialized += DoInit;
            }

            if (componentPrefab == null) {
                Debug.LogError("No MUDComponent prefab on " + gameObject.name, this);
                return;
            }

            if (componentPrefab.TableType == null) {
                Debug.LogError("No table type on " + componentPrefab.gameObject, componentPrefab);
                return;
            }

            Components = new Dictionary<string, MUDComponent>();

            if (TableDictionary.TableDict.ContainsKey(ComponentName)) {
                Debug.LogError("Bad, multiple tables of same type " + ComponentName);
                return;
            }

            if(logTable) Debug.Log("Adding " + ComponentName + " Manager");

            TableDictionary.AddTable(this);

        }

        void DoInit(NetworkManager nm) {

            if(hasInit) {
                Debug.LogError("Oh no, double Init", this);
                return;
            }

            Subscribe(net);    

            if(logTable) Debug.Log("Init: " + gameObject.name);
            
            hasInit = true;
            OnInit?.Invoke();

        }   

        protected void OnDestroy() {
            _disposers?.Dispose();
            net.OnNetworkInitialized -= DoInit;
            TableDictionary.DeleteTable(this);
            subscribe?.Dispose();
        }

        protected void Subscribe(mud.Unity.NetworkManager nm) {

            var query = new Query().In(componentPrefab.TableReference.TableId);
            subscribe = ObservableExtensions.Subscribe(net.ds.RxQuery(query).ObserveOnMainThread(), OnUpdate);
        }
        
        
        //TODO, what order do these lists update in? also what about SetField?
        private void OnUpdate((List<Record> SetRecords, List<Record> RemovedRecords) update)
        {
            SpawnInfo spawnInfo = new SpawnInfo(null, firstUpdate ? SpawnSource.Load : SpawnSource.InGame, this);
            firstUpdate = false;

            if (logTable) { Debug.Log(gameObject.name + ": " + "[Sets " + update.SetRecords?.Count + "] [Deletes " + update.RemovedRecords?.Count + "]"); }
            foreach(Record r in update.SetRecords) { IngestRecord(r, new UpdateInfo(UpdateType.SetRecord, UpdateSource.Onchain), spawnInfo);}
            foreach(Record r in update.RemovedRecords) { IngestRecord(r, new UpdateInfo(UpdateType.DeleteRecord, UpdateSource.Onchain), spawnInfo);}
        }

        public IObservable<RecordUpdate> SubscribeTable(IMudTable tableType, mud.Unity.NetworkManager nm, UpdateType updateType) {
            return NetworkManager.Instance.ds.OnDataStoreUpdate
            .Where(
                update => update.TableId == tableType.TableId.ToString() && update.Type == updateType
            )
            .Select(
                update => tableType.CreateTypedRecord(update)
            );
        }

        protected virtual void IngestRecord(Record newRecord, UpdateInfo newInfo, SpawnInfo newSpawn = null) {
            //process the table event to a key and the entity of that key
            string entityKey = newRecord.key;

            if (string.IsNullOrEmpty(entityKey)) {
                Debug.LogError("No key found in " + gameObject.name, gameObject);
                return;
            }

            IMudTable mudTable = (IMudTable)Activator.CreateInstance(componentPrefab.TableType);
            mudTable.RecordToTable(newRecord);

            IngestTable(entityKey, mudTable, newInfo, newSpawn);
        }

        protected virtual void IngestTable(string entityKey, IMudTable mudTable, UpdateInfo newInfo, SpawnInfo newSpawn = null) {

            if (string.IsNullOrEmpty(entityKey)) {
                Debug.LogError("No key found in " + gameObject.name, gameObject);
                return;
            }

            //find or spawn the entity if it doesn't exist
            MUDEntity entity = EntityDictionary.FindOrSpawnEntity(entityKey);

            //find the component
            Components.TryGetValue(entityKey, out MUDComponent component);

            //spawn the component if we can't find one
            if(component == null) {
                SpawnInfo spawn = new SpawnInfo(entity, newSpawn?.Source ?? (Loaded ? SpawnSource.InGame : SpawnSource.Load), this);
                component = entity.AddComponent(componentPrefab, spawn); 
                OnComponentSpawned?.Invoke(component);
            }

            if (logTable) { Debug.Log(gameObject.name + ": " + newInfo.UpdateType.ToString() + " , " + newInfo.Source.ToString(), component);}

            //TODO check if the update is equal to the current table, send event if it is
            //probably do this on the table itself
            //look at Record Equals() and test

            //send the update to the component
            Components[entityKey].DoUpdate(mudTable, newInfo);
            
            //delete cleanup
            if (newInfo.UpdateType == UpdateType.DeleteRecord) {
                if(deletedRecordDestroysEntity) EntityDictionary.DestroyEntity(entityKey);
            }

        }

        public void RegisterComponent(bool toggle, MUDComponent component) {
            if (toggle) {
                if (SpawnedComponents.Contains(component)) {
                    Debug.LogError("Component already added", component);
                }
                Components.Add(component.Entity.Key, component);
                SpawnedComponents.Add(component);
            } else {

                if (!SpawnedComponents.Contains(component)) {
                    Debug.LogError("Component was never added", component);
                }

                Components.Remove(component.Entity.Key);
                SpawnedComponents.Remove(component);
            }
        }


        void OnDrawGizmosSelected() {
            if(Application.isPlaying) {
                Gizmos.color = Color.blue;
                for (int i = 0; i < SpawnedComponents.Count; i++) {
                    Gizmos.DrawLine(SpawnedComponents[i].transform.position + Vector3.forward * .5f, SpawnedComponents[i].transform.position - Vector3.forward * .5f);
                    Gizmos.DrawLine(SpawnedComponents[i].transform.position + Vector3.right * .5f, SpawnedComponents[i].transform.position - Vector3.right * .5f);
                    Gizmos.DrawLine(SpawnedComponents[i].transform.position + Vector3.up * .5f, SpawnedComponents[i].transform.position - Vector3.up * .5f);
                }
            }
        }
    }

}