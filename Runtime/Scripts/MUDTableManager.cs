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

namespace mud.Client
{


    public enum UpdateEvent { Insert, Update, Delete } //Optimistic, Revert, Manual  // possible other types?
    public abstract class MUDTableManager : MUDTable
    {
        //dictionary of all entities
        public static System.Action<bool, MUDTableManager> OnTableToggle;
        public static Dictionary<string, MUDTableManager> Tables;

        public virtual System.Type ComponentType { get { return componentType; } }
        public virtual string ComponentString { get { return componentString; } }


        //dictionary of all the components this specific table has
        public Dictionary<string, MUDComponent> Components;
        public MUDComponent Prefab { get { return componentPrefab; } }

        [Header("Settings")]
        public MUDComponent componentPrefab;
        public bool deletedRecordDestroysEntity = false;
        public bool logTable = false;

        [Header("Debug")]
        public List<MUDComponent> SpawnedComponents;

        System.Type componentType;

        string componentString;
        // public Dictionary<string, MUDComponent> Components;

        protected override void Awake()
        {
            base.Awake();

            if (Tables == null)
            {
                Tables = new Dictionary<string, MUDTableManager>();
            }

            if (componentPrefab == null)
            {
                Debug.LogError("No MUDComponent prefab to spawn");
                return;
            }

            //set our table type based on the prefab we have selected
            componentType = componentPrefab.GetType();
            componentString = componentType.ToString();
            Components = new Dictionary<string, MUDComponent>();

            if (Tables.ContainsKey(ComponentType.ToString()))
            {
                Debug.LogError("Bad, multiple tables of same type " + ComponentType);
                return;
            }



        }

        protected override void Start()
        {
            base.Start();

            Debug.Log("Adding " + componentString + " Manager");

            Tables.Add(ComponentString, this);
            OnTableToggle?.Invoke(true, this);

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Tables.Remove(ComponentString);
            OnTableToggle?.Invoke(false, this);
        }

        protected override void OnInsertRecord(RecordUpdate tableUpdate)
        {
            base.OnInsertRecord(tableUpdate);
            IngestTableEvent(tableUpdate, UpdateEvent.Insert);
        }
        protected override void OnUpdateRecord(RecordUpdate tableUpdate)
        {
            base.OnUpdateRecord(tableUpdate);
            IngestTableEvent(tableUpdate, UpdateEvent.Update);
        }

        protected override void OnDeleteRecord(RecordUpdate tableUpdate)
        {
            base.OnDeleteRecord(tableUpdate);
            IngestTableEvent(tableUpdate, UpdateEvent.Delete);
        }


        protected abstract IMudTable RecordUpdateToTable(RecordUpdate tableUpdate);

        // protected virtual IMudTable RecordUpdateToTable(RecordUpdate tableUpdate)
        // {
        //     ChunkTableUpdate update = tableUpdate as ChunkTableUpdate;

        //     var currentValue = update.TypedValue.Item1;
        //     if (currentValue == null)
        //     {
        //         Debug.LogError("No currentValue");
        //         return null;
        //     }

        //     return currentValue;
        // }

        protected virtual void SpawnComponentByEntity(string entityKey)
        {
            if (string.IsNullOrEmpty(entityKey))
            {
                Debug.LogError("Empty key", gameObject);
                return;
            }


            //create the entity if it doesn't exist
            MUDEntity entity = EntityDictionary.FindOrSpawnEntity(entityKey);



        }

        protected virtual void IngestTableEvent(RecordUpdate tableUpdate, UpdateEvent eventType)
        {

            //process the table event to a key and the entity of that key
            string entityKey = tableUpdate.Key;

            if (string.IsNullOrEmpty(entityKey))
            {
                Debug.LogError("No key found in " + gameObject.name, gameObject);
                return;
            }

            MUDEntity entity = EntityDictionary.GetEntitySafe(entityKey);
            IMudTable mudTable = RecordUpdateToTable(tableUpdate);

            //create the entity if it doesn't exist
            entity = EntityDictionary.FindOrSpawnEntity(entityKey);

            if (logTable)
            {
                Debug.Log("Ingest: " + gameObject.name + " " + tableUpdate.Type.ToString() + " " + MUDHelper.TruncateHash(entityKey), entity);
            }

            if (eventType == UpdateEvent.Insert)
            {

                //create the component
                if (Components.ContainsKey(entityKey))
                {
                   
                }
                else
                {
                    MUDComponent c = entity.AddComponent(componentPrefab, this);
                }

                Components[entityKey].DoUpdate(mudTable, eventType);


            }
            else if (eventType == UpdateEvent.Update)
            {

                Components[entityKey].DoUpdate(mudTable, eventType);


            }
            else if (eventType == UpdateEvent.Delete)
            {

                Components[entityKey].DoUpdate(mudTable, eventType);
                entity.RemoveComponent(Components[entityKey]);

                if (deletedRecordDestroysEntity)
                {
                    EntityDictionary.DestroyEntity(entityKey);
                }
            }


            // if(entity != null && SpawnIfNoEntityFound && eventType == TableEvent.Delete) {
            //     DestroyEntity(entityKey);
            // }

        }

        public void RegisterComponent(bool toggle, MUDComponent component)
        {
            if (toggle)
            {
                if (SpawnedComponents.Contains(component))
                {
                    Debug.LogError("Component already added", component);
                }
                Components.Add(component.Entity.Key, component);
                SpawnedComponents.Add(component);
            }
            else
            {

                if (!SpawnedComponents.Contains(component))
                {
                    Debug.LogError("Component was never added", component);
                }

                Components.Remove(component.Entity.Key);
                SpawnedComponents.Remove(component);
            }
        }

    }
}