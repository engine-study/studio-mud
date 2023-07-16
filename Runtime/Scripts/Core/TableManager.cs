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


    public enum UpdateEvent { Insert, Update, Delete, Optimistic, Revert }  //Manual  // possible other types?
    public class TableManager : MUDTable {
        //dictionary of all entities
        public static System.Action<bool, TableManager> OnTableToggle;
        public static Dictionary<string, TableManager> Tables;

        public System.Type ComponentType { get { return componentType; } }
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

        System.Type componentType;

        string componentString;
        // public Dictionary<string, MUDComponent> Components;


        protected override void Awake() {
            base.Awake();

            if (Tables == null) {
                Tables = new Dictionary<string, TableManager>();
            }
        }

        protected override void Start() {
            base.Start();

            if (componentPrefab == null) {
                Debug.LogError("No MUDComponent prefab to spawn");
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

        protected override void OnInsertRecord(RecordUpdate tableUpdate) {
            IngestTableEvent(tableUpdate, UpdateEvent.Insert);
        }
        protected override void OnUpdateRecord(RecordUpdate tableUpdate) {
            IngestTableEvent(tableUpdate, UpdateEvent.Update);
        }

        protected override void OnDeleteRecord(RecordUpdate tableUpdate) {
            IngestTableEvent(tableUpdate, UpdateEvent.Delete);
        }

        protected override void Subscribe(mud.Unity.NetworkManager nm) {

            if (subscribeInsert) {
                IMudTable insert = (IMudTable)System.Activator.CreateInstance(componentPrefab.TableType);
                var InsertSub = ObservableExtensions.Subscribe(SubscribeTable(insert, nm, UpdateType.SetRecord).ObserveOnMainThread(), OnInsertRecord);
                _disposers.Add(InsertSub);
            }

            if (subscribeUpdate) {
                IMudTable update = (IMudTable)System.Activator.CreateInstance(componentPrefab.TableType);
                var UpdateSub = ObservableExtensions.Subscribe(SubscribeTable(update, nm, UpdateType.SetField).ObserveOnMainThread(), OnUpdateRecord);
                _disposers.Add(UpdateSub);
            }

            if (subscribeDelete) {
                IMudTable delete = (IMudTable)System.Activator.CreateInstance(componentPrefab.TableType);
                var DeleteSub = ObservableExtensions.Subscribe(SubscribeTable(delete, nm, UpdateType.DeleteRecord).ObserveOnMainThread(), OnDeleteRecord);
                _disposers.Add(DeleteSub);
            }
        }

        public static IObservable<RecordUpdate> SubscribeTable(IMudTable tableType, mud.Unity.NetworkManager nm, UpdateType updateType) {
            return NetworkManager.Instance.ds.OnDataStoreUpdate
            .Where(
                update => update.TableId == tableType.TableId.ToString() && update.Type == updateType
            )
            .Select(
                update => tableType.CreateTypedRecord(update)
            );
        }

        public IMudTable GetTableValues(MUDComponent component) {
            IMudTable table = (IMudTable)System.Activator.CreateInstance(component.TableType);
            return table.GetTableValue(component.Entity.Key);
        }


        protected virtual void SpawnComponentByEntity(string entityKey) {
            if (string.IsNullOrEmpty(entityKey)) {
                Debug.LogError("Empty key", gameObject);
                return;
            }
            //create the entity if it doesn't exist
            MUDEntity entity = EntityDictionary.FindOrSpawnEntity(entityKey);
        }

        protected virtual void IngestTableEvent(RecordUpdate tableUpdate, UpdateEvent eventType) {

            //process the table event to a key and the entity of that key
            string entityKey = tableUpdate.Key;

            if (string.IsNullOrEmpty(entityKey)) {
                Debug.LogError("No key found in " + gameObject.name, gameObject);
                return;
            }

            MUDEntity entity = EntityDictionary.GetEntitySafe(entityKey);

            IMudTable mudTable = (IMudTable)System.Activator.CreateInstance(componentPrefab.TableType);
            mudTable = mudTable.RecordUpdateToTable(tableUpdate);

            //create the entity if it doesn't exist
            entity = EntityDictionary.FindOrSpawnEntity(entityKey);

            if (logTable) {
                Debug.Log("Ingest: " + gameObject.name + " " + tableUpdate.Type.ToString() + " " + MUDHelper.TruncateHash(entityKey), entity);
            }

            if (eventType == UpdateEvent.Insert) {

                //create the component if we can't find it
                if (Components.ContainsKey(entityKey)) { } else { MUDComponent c = entity.AddComponent(componentPrefab, this); }

                Components[entityKey].DoUpdate(mudTable, eventType);


            } else if (eventType == UpdateEvent.Update) {

                Components[entityKey].DoUpdate(mudTable, eventType);


            } else if (eventType == UpdateEvent.Delete) {

                Components[entityKey].DoUpdate(mudTable, eventType);
                entity.RemoveComponent(Components[entityKey]);

                if (deletedRecordDestroysEntity) {
                    EntityDictionary.DestroyEntity(entityKey);
                }
            }


            // if(entity != null && SpawnIfNoEntityFound && eventType == TableEvent.Delete) {
            //     DestroyEntity(entityKey);
            // }

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