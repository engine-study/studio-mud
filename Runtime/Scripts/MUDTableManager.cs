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


    public enum UpdateEvent { Insert, Update, Delete, Optimistic, Revert, Manual }
    public abstract class MUDTableManager : MUDTable
    {
        //dictionary of all entities
        public static Dictionary<string, MUDTableManager> Tables;

        public virtual System.Type ComponentType { get { return componentType; } }
        public virtual string ComponentString { get { return componentString; } }


        //dictionary of all the components this specific table has
        public Dictionary<string, MUDComponent> Components;
        public List<MUDComponent> SpawnedComponents;
        public MUDComponent Prefab { get { return componentPrefab; } }

        [Header("Settings")]
        public MUDComponent componentPrefab;


        [Header("Options")]
        public bool deletedRecordDestroysEntity = false;



        System.Type componentType;

        string componentString;
        // public Dictionary<string, MUDComponent> Components;

        protected override void Awake()
        {
            base.Awake();

            componentType = componentPrefab.GetType();
            componentString = componentType.ToString();

            if (componentPrefab == null)
            {
                Debug.LogError("No MUDComponent prefab to spawn");
                return;
            }

            if (Tables == null)
            {
                Tables = new Dictionary<string, MUDTableManager>();
            }

            if (Tables.ContainsKey(ComponentType.ToString()))
            {
                Debug.LogError("Fatal error, multiple tables of same type " + ComponentType);
            }

            Debug.Log("Adding " + componentString + " Manager");
            Tables.Add(ComponentType.ToString(), this);

            Components = new Dictionary<string, MUDComponent>();
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

        protected virtual void IngestTableEvent(RecordUpdate tableUpdate, UpdateEvent eventType)
        {

            //process the table event to a key and the entity of that key
            string entityKey = tableUpdate.Key;

            // Debug.Log("Ingest: " + gameObject.name + " " + eventType.ToString(),gameObject);

            if (string.IsNullOrEmpty(entityKey))
            {
                Debug.LogError("No key found in " + gameObject.name, gameObject);
                return;
            }

            MUDEntity entity = EntityDictionary.GetEntitySafe(entityKey);
            IMudTable mudTable = RecordUpdateToTable(tableUpdate);

            if (eventType == UpdateEvent.Insert)
            {

                //create the entity if it doesn't exist
                entity = EntityDictionary.FindOrSpawnEntity(entityKey);

                //create the component
                if (!Components.ContainsKey(entityKey))
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

        protected virtual void UpdateComponent(MUDComponent update, UpdateEvent eventType)
        {

            if (eventType == UpdateEvent.Insert)
            {

            }
            else if (eventType == UpdateEvent.Delete)
            {

            }
            else if (eventType == UpdateEvent.Update)
            {

            }
        }

    }
}