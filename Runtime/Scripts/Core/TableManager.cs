using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud;
using NetworkManager = mud.NetworkManager;
using UniRx;
using ObservableExtensions = UniRx.ObservableExtensions;
using System.Threading.Tasks;
using UnityEditor;
using Newtonsoft.Json;
using Property = System.Collections.Generic.Dictionary<string, object>;

namespace mud {


    public class TableManager : MonoBehaviour {

        public Action<MUDComponent> OnComponentSpawned, OnComponentUpdated;
        public bool Loaded {get{return hasInit;}}


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
        [SerializeField] bool hasSpawned = false;
        [SerializeField] bool hasInit = false;
        IDisposable _sub;


        // public Dictionary<string, MUDComponent> Components;

        void Start() {
            if(!hasInit) DoInit();
        }

        void OnDestroy() {
            _sub?.Dispose();
            NetworkManager.OnInitialized -= Spawn;
            TableDictionary.DeleteTable(this);
        }

        void DoInit() {

            if(hasInit) { Debug.LogError("Double Init", this);return; }

            components = new List<MUDComponent>();
            ComponentDict = new Dictionary<string, MUDComponent>();

            hasInit = true;

            if(AutoSpawn) {
                if(NetworkManager.Initialized) { DoAutoSpawn(); } 
                else { NetworkManager.OnInitialized += DoAutoSpawn; }
            }

        }   

        public void DoAutoSpawn() {
            Spawn(componentPrefab);
        }
        public void Spawn(MUDComponent prefab) {
            SetPrefab(prefab);
            Spawn();
        }

        public void SetPrefab(MUDComponent newPrefab) {
            gameObject.name = newPrefab.MUDTableName;
            componentPrefab = newPrefab;
            #if UNITY_EDITOR
            Debug.Assert(PrefabUtility.IsPartOfPrefabAsset(newPrefab), "Please connect the " + newPrefab.gameObject.name + " prefab from your Project window, not from a " + newPrefab.gameObject.name + " in the scene.", this); 
            #endif
        }

        public void Spawn() {

            if(!hasInit) { DoInit(); }
            if (componentPrefab == null) { Debug.LogError("No MUDComponent prefab on " + gameObject.name, this);return;}
            if (componentPrefab.MUDTableType == null) { Debug.LogError("No table type on " + componentPrefab.gameObject, componentPrefab);return;}
            if(hasSpawned) { Debug.LogError(componentPrefab.name + "already subscribed", this); return; }
            if (TableDictionary.TableDict.ContainsKey(ComponentName)) { Debug.LogError($"Registered {ComponentName} multiple times.", this); return;}

            if(LogTable) Debug.Log("[TABLE] " + "Subscribe: " + componentPrefab.name, this);
            TableDictionary.AddTable(this);

            // _counterSub = IMudTable.GetUpdates<CounterTable>().ObserveOnMainThread().Subscribe(OnIncrement);
            _sub = IMudTable.GetUpdates(componentPrefab.TableReference.TableType()).ObserveOnMainThread().Subscribe(IngestUpdate);

            hasSpawned = true;
        }

 

        void IngestUpdate(RecordUpdate update) {

            Debug.Log($"Update: {JsonConvert.SerializeObject(update)}");

            //process the table event to a key and the entity of that key
            string entityKey = update.CurrentRecordKey;
            Property p = (Property)update.CurrentRecordValue;
            IMudTable mudTable = (IMudTable)Activator.CreateInstance(componentPrefab.MUDTableType);
            mudTable.PropertyToTable(p);

            if (string.IsNullOrEmpty(entityKey)) { Debug.LogError("No key " + gameObject.name, this); return;}

            UpdateInfo info = new UpdateInfo(update.Type, UpdateSource.Onchain);
            SpawnInfo spawn = new SpawnInfo(null, hasSpawned ? SpawnSource.Load : SpawnSource.InGame, this);

            IngestTable(entityKey, mudTable, info, spawn);
        }

        void IngestTable(string entityKey, IMudTable mudTable, UpdateInfo newInfo, SpawnInfo newSpawn = null) {

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
                SpawnInfo spawn = new SpawnInfo(entity, newSpawn?.Source ?? (Loaded ? SpawnSource.InGame : SpawnSource.Load), this);
                component = entity.AddComponent(componentPrefab, spawn); 
                wasSpawned = true;
            }

            if (LogTable) { Debug.Log(component.Entity.Name + " [TABLE] " + gameObject.name.ToUpper() + ": " + newInfo.UpdateType.ToString() + " , " + newInfo.Source.ToString(), component);}

            //TODO check if the update is equal to the current table, send event if it is
            //probably do this on the table itself
            //look at RxRecord Equals() and test

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