using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud;
using UniRx;
using ObservableExtensions = UniRx.ObservableExtensions;
using System.Threading.Tasks;
using UnityEditor;
using Newtonsoft.Json;
using Property = System.Collections.Generic.Dictionary<string, object>;
using Cysharp.Threading.Tasks;

namespace mud {


    public class TableManager : MonoBehaviour {

        public static TableManager LatestTable;
        public Action<MUDComponent> OnComponentSpawned, OnComponentUpdated;
        public static Action<TableManager> OnTableRegistered;
        public static Action<TableManager> OnTableLoading;
        public bool Loaded {get{return hasSpawned;}}


        //dictionary of all entities        
        public Type ComponentType { get { return componentPrefab.GetType(); } }
        public string ComponentName { get { return componentPrefab.MUDTableName; } }
        public List<MUDComponent> Components { get { return components; } }
        public MUDComponent Prefab { get { return componentPrefab; } }
        public Dictionary<string, MUDComponent> ComponentDict;

        //dictionary of all the components this specific table has
        public bool EntityHasComponent(string key) { return ComponentDict.ContainsKey(key); }
        public MUDComponent EntityToComponent(string key) { return ComponentDict[key]; }

        [Header("Required")]
        [SerializeField] MUDComponent componentPrefab;

        [Header("Behaviour")]
        [SerializeField] public bool AutoSpawn = true;
        public bool LogTable = false;

        [Header("Debug")]
        [SerializeField] List<MUDComponent> components;
        [SerializeField] bool hasRegistered = false;
        [SerializeField] bool hasSpawned = false;
        IDisposable _sub;


        // public Dictionary<string, MUDComponent> Components;

        void Start() {
            if(!hasRegistered) RegisterTable(Prefab);
        }

        void OnDestroy() {
            _sub?.Dispose();
            NetworkManager.OnInitialized -= Spawn;
            TableDictionary.DeleteTable(this);
        }

        public void RegisterTable(MUDComponent newPrefab) {

            if(hasRegistered) { Debug.LogError("Double Init", this);return; }

            components = new List<MUDComponent>();
            ComponentDict = new Dictionary<string, MUDComponent>();

            componentPrefab = newPrefab;
            
            //check that we have a legit prefab
            if (Prefab == null) { Debug.LogError("No MUDComponent prefab on " + gameObject.name, this);return;}
            if (Prefab.MUDTableType == null) { Debug.LogError("No table type on " + Prefab.gameObject, Prefab);return;}

            gameObject.name = Prefab.MUDTableName;

            if(LogTable) Debug.Log($"[TABLE {Prefab.MUDTableType}] Added.", this);

            #if UNITY_EDITOR
            Debug.Assert(PrefabUtility.IsPartOfPrefabAsset(newPrefab), "Please connect the " + newPrefab.gameObject.name + " prefab from your Project window, not from a " + newPrefab.gameObject.name + " in the scene.", this); 
            #endif

            if(hasSpawned) { Debug.LogError(Prefab.name + "already subscribed", this); return; }
            if (TableDictionary.TableDict.ContainsKey(ComponentName)) { Debug.LogError($"Registered {ComponentName} multiple times.", this); return;}
            if (TableDictionary.ComponentManagerDict.ContainsKey(Prefab.GetType())) { Debug.LogError($"Registered {Prefab.GetType()} multiple times.", this); return;}
            if (TableDictionary.TableManagerDict.ContainsKey(Prefab.MUDTableType)) { Debug.LogError($"Registered {Prefab.MUDTableType} multiple times.", this); return;}

            //Add the table to global table list
            TableDictionary.AddTable(this);            
            hasRegistered = true;

            OnTableRegistered?.Invoke(this);

            if(AutoSpawn) {
                if(NetworkManager.Initialized) { DoAutoSpawn(); } 
                else { NetworkManager.OnInitialized += DoAutoSpawn; }
            }

        }   

        public void DoAutoSpawn() { Spawn(componentPrefab);}
        public void Spawn() { Spawn(Prefab);}

        public void Spawn(MUDComponent prefab) {

            if(hasRegistered) { if(prefab != Prefab) {Debug.LogError("Already init, can't change prefab"); return;}}
            else {RegisterTable(prefab);}

            LatestTable = this;

            // _counterSub = IMudTable.GetUpdates<CounterTable>().ObserveOnMainThread().Subscribe(OnIncrement);
            _sub = IMudTable.GetUpdates(componentPrefab.TableReference.TableType()).ObserveOnMainThread().Subscribe(Ingest);

            StartCoroutine(SetSpawnedAtEndOfFrame());

        }

        IEnumerator SetSpawnedAtEndOfFrame() {
            yield return null;
            hasSpawned = true;
            if(LogTable) Debug.Log($"[TABLE {componentPrefab.name}] Spawned.", this);
        }

        void Ingest(RecordUpdate update) {

            //process the table event to a key and the entity of that key
            string entityKey = update.CurrentRecordKey;
            if (string.IsNullOrEmpty(entityKey)) { Debug.LogError("No key " + gameObject.name, this); return;}

            //deep logging
            // if (LogTable) {Debug.Log($"Key: {entityKey}", this);}
            // if (LogTable) {Debug.Log($"Update: {JsonConvert.SerializeObject(update)}", this);}
            IMudTable mudTable = null;
            
            if(update.Type == UpdateType.SetRecord || update.Type == UpdateType.SetField) {
                Property p = (Property)update.CurrentRecordValue;
                mudTable = (IMudTable)Activator.CreateInstance(componentPrefab.MUDTableType);
                mudTable.PropertyToTable(p);
            } 

            UpdateInfo info = new UpdateInfo(update.Type, UpdateSource.Onchain);

            IngestTable(entityKey, mudTable, info);
        }

        void IngestTable(string entityKey, IMudTable mudTable, UpdateInfo newInfo) {

            if (string.IsNullOrEmpty(entityKey)) {
                Debug.LogError("No key found in " + gameObject.name, gameObject);
                return;
            }

            //find or spawn the entity if it doesn't exist
            MUDEntity entity = EntityDictionary.FindOrSpawnEntity(entityKey);

            //find the component
            ComponentDict.TryGetValue(entityKey, out MUDComponent component);

            //spawn the component if we can't find one
            bool wasSpawned = false;
            if(component == null) {
                SpawnInfo spawn = new SpawnInfo(entity, hasSpawned ? SpawnSource.InGame : SpawnSource.Load, this);
                component = entity.AddComponent(componentPrefab, spawn); 
                wasSpawned = true;
            }

            //TODO check if the update is equal to the current table, send event if it is
            //probably do this on the table itself
            //look at RxRecord Equals() and test
            if (LogTable) { Debug.Log($"[{gameObject.name}] {component.Entity.gameObject.name} [{newInfo.UpdateType}]", component);}

            //send the update to the component
            ComponentDict[entityKey].DoUpdate(mudTable, newInfo);


            if(wasSpawned) {OnComponentSpawned?.Invoke(component);}
            OnComponentUpdated?.Invoke(component);


            // //delete cleanup
            // if (newInfo.UpdateType == UpdateType.DeleteRecord) {
            //     if(deletedRecordDestroysEntity) EntityDictionary.DestroyEntity(entityKey);
            // }

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