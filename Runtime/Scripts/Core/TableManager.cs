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


    public class TableManager : MUDTable {
        //dictionary of all entities
        public static Action<bool, TableManager> OnTableToggle;
        public Action<bool, MUDComponent> OnComponentToggle;
        public static Dictionary<string, TableManager> Tables;
        
        public Type ComponentType { get { return componentType; } }
        public string ComponentString { get { return componentString; } }


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

        Type componentType;

        string componentString;
        // public Dictionary<string, MUDComponent> Components;


        protected override void Awake() {
            base.Awake();

            SpawnedComponents = new List<MUDComponent>();

            if (Tables == null) {
                Tables = new Dictionary<string, TableManager>();
            }
        }

        protected override void Start() {
            base.Start();

            if (componentPrefab == null) {
                Debug.LogError("No MUDComponent prefab on " + gameObject.name, this);
                return;
            }

            if (componentPrefab.TableType == null) {
                Debug.LogError("No table type on " + componentPrefab.gameObject, componentPrefab);
                return;
            }

            //set our table type based on the prefab we have selected
            componentType = componentPrefab.GetType();
            componentString = componentPrefab.GetType().ToString();

            Components = new Dictionary<string, MUDComponent>();

            if (Tables.ContainsKey(ComponentType.ToString())) {
                Debug.LogError("Bad, multiple tables of same type " + ComponentType);
                return;
            }

            Debug.Log("Adding " + componentString + " Manager");

            Tables.Add(ComponentString, this);
            OnTableToggle?.Invoke(true, this);

        }

        protected override void OnDestroy() {
            base.OnDestroy();

            Tables.Remove(ComponentString);
            OnTableToggle?.Invoke(false, this);
        }

        protected override void Subscribe(mud.Unity.NetworkManager nm) {

            if (subscribeInsert) {
                IMudTable insert = (IMudTable)Activator.CreateInstance(componentPrefab.TableType);
                var InsertSub = ObservableExtensions.Subscribe(SubscribeTable(insert, nm, UpdateType.SetRecord).ObserveOnMainThread(), OnInsertRecord);
                _disposers.Add(InsertSub);
            }

            if (subscribeUpdate) {
                IMudTable update = (IMudTable)Activator.CreateInstance(componentPrefab.TableType);
                var UpdateSub = ObservableExtensions.Subscribe(SubscribeTable(update, nm, UpdateType.SetField).ObserveOnMainThread(), OnUpdateRecord);
                _disposers.Add(UpdateSub);
            }

            if (subscribeDelete) {
                IMudTable delete = (IMudTable)Activator.CreateInstance(componentPrefab.TableType);
                var DeleteSub = ObservableExtensions.Subscribe(SubscribeTable(delete, nm, UpdateType.DeleteRecord).ObserveOnMainThread(), OnDeleteRecord);
                _disposers.Add(DeleteSub);
            }
        }

        void OnInsertRecord(RecordUpdate tableUpdate) {IngestRecord(tableUpdate, new UpdateInfo(UpdateType.SetRecord, UpdateSource.Onchain));}
        void OnUpdateRecord(RecordUpdate tableUpdate) {IngestRecord(tableUpdate, new UpdateInfo(UpdateType.SetField, UpdateSource.Onchain));}
        void OnDeleteRecord(RecordUpdate tableUpdate) {IngestRecord(tableUpdate, new UpdateInfo(UpdateType.DeleteRecord, UpdateSource.Onchain));}

        public static IObservable<RecordUpdate> SubscribeTable(IMudTable tableType, mud.Unity.NetworkManager nm, UpdateType updateType) {
            return NetworkManager.Instance.ds.OnDataStoreUpdate
            .Where(
                update => update.TableId == tableType.TableId.ToString() && update.Type == updateType
            )
            .Select(
                update => tableType.CreateTypedRecord(update)
            );
        }

        public static T FindComponent<T>(string entity) where T : MUDComponent {

            //try to find the tablemanager
            TableManager tm = FindTable<T>();
            MUDComponent component = null;
            tm.Components.TryGetValue(entity, out component);
            return component as T;
        }

        public static T FindOrMakeComponent<T>(string entityKey) where T : MUDComponent {

            //try to find the tablemanager
            TableManager tm = FindTable<T>();
            MUDEntity entity = EntityDictionary.FindOrSpawnEntity(entityKey);
            MUDComponent component = entity.AddComponent<T>(ComponentDictionary.FindPrefab<T>(), tm);
            return component as T;
        }


        public static TableManager FindTable<T>() where T : MUDComponent {
            TableManager tm = Tables[typeof(T).Name];
            if (tm == null) { Debug.LogError("Could not find " + typeof(T).Name + " table"); }
            return tm;
        }

        public static T FindValue<T>(string entityKey) where T : IMudTable, new() {
            T table = new T();
            // IMudTable table = (IMudTable)Activator.CreateInstance(component.TableType);
            return IMudTable.GetValueFromTable<T>(entityKey);
        }

        protected virtual void IngestRecord(RecordUpdate tableUpdate, UpdateInfo newInfo) {
            //process the table event to a key and the entity of that key
            string entityKey = tableUpdate.Key;

            if (string.IsNullOrEmpty(entityKey)) {
                Debug.LogError("No key found in " + gameObject.name, gameObject);
                return;
            }

            IMudTable mudTable = (IMudTable)Activator.CreateInstance(componentPrefab.TableType);
            mudTable = mudTable.RecordUpdateToTable(tableUpdate);

            IngestTable(entityKey, mudTable, newInfo);
        }

        protected virtual void IngestTable(string entityKey, IMudTable mudTable, UpdateInfo newInfo) {

            if (string.IsNullOrEmpty(entityKey)) {
                Debug.LogError("No key found in " + gameObject.name, gameObject);
                return;
            }

            //create the entity if it doesn't exist
            MUDEntity entity = EntityDictionary.FindOrSpawnEntity(entityKey);

            //find the component
            MUDComponent component = null;
            Components.TryGetValue(entityKey, out component);

            //add a component if we can't find one
            if(component == null) {
                component = entity.AddComponent(componentPrefab, this); 
                OnComponentToggle?.Invoke(true, component);
            }

            if (logTable) { Debug.Log(gameObject.name + ": " + newInfo.UpdateType.ToString() + " , " + newInfo.UpdateSource.ToString(), component);}

            //send the UDPATE!
            Components[entityKey].DoUpdate(mudTable, newInfo);
            
            //delete cleanup
            if (newInfo.UpdateType == UpdateType.DeleteRecord && deletedRecordDestroysEntity) {
                EntityDictionary.DestroyEntity(entityKey);
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

    }
}