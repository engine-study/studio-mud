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

public abstract class MUDTableManager : MUDTable
{
    //dictionary of all entities
    public static Dictionary<int, MUDTableManager> Tables;
    public static Dictionary<string, MUDEntity> Entities;
    public static int AddedTables = 0;


    [Header("Table")]
    public MUDComponent componentPrefab;
    [Header("Options")]
    public bool deletedRecordDestroysEntity = false;


    //dictionary of all the components this specific table has
    public Dictionary<string, MUDComponent> Components;
    // public Dictionary<string, MUDComponent> Components;

    protected override void Awake()
    {
        base.Awake();

        if(componentPrefab == null) {
            Debug.LogError("No MUDComponent prefab to spawn");
            return;
        }

        if(Tables == null) {
            Tables = new Dictionary<int, MUDTableManager>();
        }

        Tables.Add(AddedTables,this);
        AddedTables++;

        if(Entities == null) {
            Entities = new Dictionary<string, MUDEntity>();
        }

        Components = new Dictionary<string, MUDComponent>();
    }

    protected override void OnInsertRecord(RecordUpdate tableUpdate)
    {
        base.OnInsertRecord(tableUpdate);
        IngestTableEvent(tableUpdate, TableEvent.Insert);
    }
    protected override void OnUpdateRecord(RecordUpdate tableUpdate)
    {
        base.OnUpdateRecord(tableUpdate);
        IngestTableEvent(tableUpdate, TableEvent.Update);
    }

    protected override void OnDeleteRecord(RecordUpdate tableUpdate)
    {
        base.OnDeleteRecord(tableUpdate);
        IngestTableEvent(tableUpdate, TableEvent.Delete);
    }



    protected abstract IMudTable RecordUpdateToTable(RecordUpdate tableUpdate);

    protected virtual void IngestTableEvent(RecordUpdate tableUpdate, TableEvent eventType) {

        //process the table event to a key and the entity of that key
        string entityKey = tableUpdate.Key;

        // Debug.Log("Ingest: " + gameObject.name + " " + eventType.ToString(),gameObject);

        if(string.IsNullOrEmpty(entityKey)) {
            Debug.LogError("No key found in " + gameObject.name, gameObject);
            return;
        }

        MUDEntity entity = EntityDictionary.GetEntitySafe(entityKey);
        IMudTable mudTable = RecordUpdateToTable(tableUpdate);

        if(eventType == TableEvent.Insert) {
            //create the entity if it doesn't exist
            entity = EntityDictionary.FindOrSpawnEntity(entityKey);

            //create the component
            if(!Components.ContainsKey(entityKey)) {
                MUDComponent c = entity.AddComponent(componentPrefab, this);
            }
            Components[entityKey].UpdateComponent(mudTable, eventType);
        } else if(eventType == TableEvent.Update) {
            Components[entityKey].UpdateComponent(mudTable, eventType);
        } else if(eventType == TableEvent.Delete) {
            entity.RemoveComponent(Components[entityKey]);

            if(deletedRecordDestroysEntity) {
                EntityDictionary.DestroyEntity(entityKey);
            }
        }


        // if(entity != null && SpawnIfNoEntityFound && eventType == TableEvent.Delete) {
        //     DestroyEntity(entityKey);
        // }
        
    }

    protected virtual void UpdateComponent(MUDComponent update, TableEvent eventType) {

         if(eventType == TableEvent.Insert) {

        } else if(eventType == TableEvent.Delete) {

        } else if(eventType == TableEvent.Update) {
            
        }
    }




}
