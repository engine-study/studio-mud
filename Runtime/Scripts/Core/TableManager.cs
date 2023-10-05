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
using UnityEditor;

namespace mud.Client {


    public class TableManager : MonoBehaviour {

        public Action<MUDComponent> OnComponentSpawned, OnComponentUpdated;
        public bool Loaded {get{return hasInit;}}
        public Action OnInit;
        public Action OnAdded, OnUpdated, OnDeleted;


        //dictionary of all entities        
        public Type ComponentType { get { return componentPrefab.GetType(); } }
        public string ComponentName { get { return componentPrefab.MUDTableName; } }
        public List<MUDComponent> Components { get { return components; } }
        public MUDComponent Prefab { get { return componentPrefab; } }
        public Dictionary<string, MUDComponent> ComponentDict;

        //dictionary of all the components this specific table has
        public bool EntityHasComponent(string key) { return ComponentDict.ContainsKey(key); }
        public MUDComponent EntityToComponent(string key) { return ComponentDict[key]; }

        [Header("Settings")]
        [SerializeField] MUDComponent componentPrefab;
        [SerializeField] bool subscribeInsert = true, subscribeUpdate = true, subscribeDelete = true;

        [Header("Behaviour")]
        [SerializeField] public bool AutoSpawn = true;
        [SerializeField] bool deletedRecordDestroysEntity = false;

        [Header("Debug")]
        public bool LogTable = false;
        [SerializeField] List<MUDComponent> components;

        CompositeDisposable _disposers = new();
        bool hasInit;
        bool firstUpdate = true;

        // public Dictionary<string, MUDComponent> Components;

        void Start() {

            if(NetworkManager.Initialized) { DoInit(); } 
            else { NetworkManager.OnInitialized += DoInit; }
        
        }

        void DoInit() {

            if(hasInit) { Debug.LogError("Oh no, double Init", this);return; }
            if (componentPrefab == null) { Debug.LogError("No MUDComponent prefab on " + gameObject.name, this);return;}
            if (componentPrefab.MUDTableType == null) { Debug.LogError("No table type on " + componentPrefab.gameObject, componentPrefab);return;}
            if (TableDictionary.TableDict.ContainsKey(ComponentName)) { Debug.LogError("Bad, multiple tables of same type " + ComponentName); return;}

            components = new List<MUDComponent>();
            ComponentDict = new Dictionary<string, MUDComponent>();

            TableDictionary.AddTable(this);

            if(LogTable) Debug.Log("[TABLE] " + "Init: " + gameObject.name);

            hasInit = true;
            OnInit?.Invoke();

            if(AutoSpawn) {
                Spawn(componentPrefab);
            }

        }   

        public void Spawn(MUDComponent prefab) {

            if(!hasInit) { Debug.LogError("Has to init", this);return; }

            gameObject.name = prefab.MUDTableName;
            AutoSpawn = false;

            SetPrefab(prefab);
            SubscribeAll();    

        }

        //PrefabUtility.GetPrefabType into PrefabUtility.GetPrefabAssetType and PrefabUtility.GetPrefabInstanceStatus.
        //TODO check to see if the prefab set in Editor is an instance or not, give a warning if it is
        public void SetPrefab(MUDComponent newPrefab) {
            componentPrefab = newPrefab;
            #if UNITY_EDITOR
            Debug.Assert(PrefabUtility.IsPartOfPrefabAsset(newPrefab), "Please connect the " + newPrefab.gameObject.name + " prefab from your Project window, not from a " + newPrefab.gameObject.name + " in the scene.", this); 
            #endif
        }

        

        void OnDestroy() {
            _disposers?.Dispose();
            NetworkManager.OnInitialized -= DoInit;
            TableDictionary.DeleteTable(this);
        }

        //loads a chunk and updates them (TODO how do we prevent double subscribes?)
        public void SubscribeAll() {
            var query = new Query().In(componentPrefab.TableReference.TableId);
            _disposers.Add(ObservableExtensions.Subscribe(NetworkManager.Instance.ds.RxQuery(query).ObserveOnMainThread(), OnUpdate));        
        }

        public void Subscribe(Query query) {
            _disposers.Add(ObservableExtensions.Subscribe(NetworkManager.Instance.ds.RxQuery(query).ObserveOnMainThread(), OnUpdate));
        }

        //old method        
        // public IObservable<RecordUpdate> SubscribeTable(IMudTable tableType, mud.Unity.NetworkManager nm, UpdateType updateType) {
        //     return NetworkManager.Instance.ds.OnDataStoreUpdate
        //     .Where(update => update.TableId == tableType.TableId.ToString() && update.Type == updateType)
        //     .Select( update => tableType.CreateTypedRecord(update) );
        // }
        
        //TODO, what order do these lists update in? also what about SetField?
        void OnUpdate((List<Record> SetRecords, List<Record> RemovedRecords) update) {

            SpawnInfo spawnInfo = new SpawnInfo(null, firstUpdate ? SpawnSource.Load : SpawnSource.InGame, this);
            firstUpdate = false;

            // if (LogTable) { Debug.Log("[TABLE] " + gameObject.name + ": " + "[Sets " + update.SetRecords?.Count + "] [Deletes " + update.RemovedRecords?.Count + "]"); }
            foreach(Record r in update.SetRecords) { IngestRecord(r, new UpdateInfo(UpdateType.SetRecord, UpdateSource.Onchain), spawnInfo);}
            foreach(Record r in update.RemovedRecords) { IngestRecord(r, new UpdateInfo(UpdateType.DeleteRecord, UpdateSource.Onchain), spawnInfo);}
        }

        protected virtual void IngestRecord(Record newRecord, UpdateInfo newInfo, SpawnInfo newSpawn = null) {
            //process the table event to a key and the entity of that key
            string entityKey = newRecord.key;

            if (string.IsNullOrEmpty(entityKey)) {
                Debug.LogError("No key found in " + gameObject.name, gameObject);
                return;
            }

            IMudTable mudTable = (IMudTable)Activator.CreateInstance(componentPrefab.MUDTableType);
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
            ComponentDict.TryGetValue(entityKey, out MUDComponent component);

            //spawn the component if we can't find one
            if(component == null) {
                SpawnInfo spawn = new SpawnInfo(entity, newSpawn?.Source ?? (Loaded ? SpawnSource.InGame : SpawnSource.Load), this);
                component = entity.AddComponent(componentPrefab, spawn); 
                OnComponentSpawned?.Invoke(component);
            }

            if (LogTable) { Debug.Log(component.Entity.Name + " [TABLE] " + gameObject.name + ": " + newInfo.UpdateType.ToString() + " , " + newInfo.Source.ToString(), component);}

            //TODO check if the update is equal to the current table, send event if it is
            //probably do this on the table itself
            //look at Record Equals() and test

            //send the update to the component
            ComponentDict[entityKey].DoUpdate(mudTable, newInfo);
            OnComponentUpdated?.Invoke(component);

            //delete cleanup
            if (newInfo.UpdateType == UpdateType.DeleteRecord) {
                if(deletedRecordDestroysEntity) EntityDictionary.DestroyEntity(entityKey);
            }

        }

        public virtual void RegisterComponent(bool toggle, MUDComponent component) {
            if (toggle) {
                if (components.Contains(component)) {
                    Debug.LogError("Component already added", component);
                }
                ComponentDict.Add(component.Entity.Key, component);
                components.Add(component);
            } else {

                if (!components.Contains(component)) {
                    Debug.LogError("Component was never added", component);
                }

                ComponentDict.Remove(component.Entity.Key);
                components.Remove(component);
            }
        }


        void OnDrawGizmosSelected() {
            if(Application.isPlaying) {
                Gizmos.color = Color.blue;
                for (int i = 0; i < components.Count; i++) {
                    Gizmos.DrawLine(components[i].transform.position + Vector3.forward * .5f, components[i].transform.position - Vector3.forward * .5f);
                    Gizmos.DrawLine(components[i].transform.position + Vector3.right * .5f, components[i].transform.position - Vector3.right * .5f);
                    Gizmos.DrawLine(components[i].transform.position + Vector3.up * .5f, components[i].transform.position - Vector3.up * .5f);
                }
            }
        }
    }

}