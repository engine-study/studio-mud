using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud
{


    public class MUDWorld : MonoBehaviour
    {
        public static MUDEntity FindEntity(string entity) {
            return EntityDictionary.FindEntitySafe(entity);
        }

     
        public static MUDComponent FindComponent<T>(string entity) where T : MUDTable, new() { 
            //try to find the tablemanager
            TableManager tm = GetManager<T>();
            MUDComponent component = null;
            tm?.ComponentDict.TryGetValue(entity, out component);
            return component;
        }

        //necessary in cases where Components have been reused for multiple tables (ie. )
        //we only let them search components through the tables
        public static C FindComponent<T, C>(string entity) where T : MUDTable, new() where C : MUDComponent, new(){  return (C)FindComponent<T>(entity);}

        public static C FindOrMakeComponent<T,C>(MUDEntity entity) where T : MUDTable, new() where C : MUDComponent, new() { return (C)FindOrMakeComponent<T>(entity.Key); }
        public static MUDComponent FindOrMakeComponent<T>(MUDEntity entity) where T : MUDTable, new() { return FindOrMakeComponent<T>(entity.Key); }
        public static MUDComponent FindOrMakeComponent<T>(string entityKey) where T : MUDTable, new() {

            //try to find the tablemanager
            TableManager tm = GetManager<T>();
            if(tm == null) {Debug.LogError(typeof(T).Name + ": Could not find Table"); return null;}
            
            MUDComponent prefab = tm.Prefab;
            MUDEntity entity = EntityDictionary.FindOrSpawnEntity(entityKey);
            SpawnInfo si = new SpawnInfo(entity, SpawnSource.InGame, tm);
            MUDComponent component = entity.AddComponent(prefab, si);
            return component;
        }


        public static TableManager GetManager<T>() where T : MUDTable { TableDictionary.TableDict.TryGetValue(typeof(T), out TableManager tm); return tm; }
        public static TableManager GetManager(MUDTable mudTable) {return GetManager(mudTable.GetType()); }
        public static TableManager GetManager(Type mudTable) {TableDictionary.TableDict.TryGetValue(mudTable, out TableManager tm); return tm; }

        public static T GetTable<T>(string entityKey) where T : MUDTable, new() {
            T table = new T();
            // IMudTable table = (IMudTable)Activator.CreateInstance(component.TableType);
            return MUDTable.GetTable<T>(entityKey);
        }

        // public static T FindPrefab<T>() where T : MUDComponent, new() { return (T)(FindTable<T>()?.Prefab);}
        // public static MUDComponent FindPrefab(IMudTable table) { return FindTableByMUDTable(table)?.Prefab;}

    }
}