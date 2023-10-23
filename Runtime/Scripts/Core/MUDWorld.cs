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

        public static T FindComponent<T>(string entity) where T : MUDComponent, new() {
            //try to find the tablemanager
            TableManager tm = FindTable<T>();
            MUDComponent component = null;
            tm?.ComponentDict.TryGetValue(entity, out component);
            return (T)component;
        }

        public static T FindOrMakeComponent<T>(MUDEntity entity) where T : MUDComponent, new() { return FindOrMakeComponent<T>(entity.Key); }
        public static T FindOrMakeComponent<T>(string entityKey) where T : MUDComponent, new() {

            //try to find the tablemanager
            TableManager tm = FindTable<T>();
            if(tm == null) {Debug.LogError(typeof(T).Name + ": Could not find Table"); return null;}
            
            MUDComponent prefab = tm.Prefab;
            MUDEntity entity = EntityDictionary.FindOrSpawnEntity(entityKey);
            SpawnInfo si = new SpawnInfo(entity, SpawnSource.InGame, tm);
            MUDComponent component = entity.AddComponent(prefab, si);
            return component as T;
        }


        public static TableManager FindTable<T>() where T : MUDComponent {TableDictionary.ComponentManagerDict.TryGetValue(typeof(T), out TableManager tm); return tm;}
        public static TableManager FindTable<T>(T c) where T : MUDComponent {TableDictionary.ComponentManagerDict.TryGetValue(c.GetType(), out TableManager tm); return tm;}
        public static TableManager FindTable(Type componentType) {TableDictionary.ComponentManagerDict.TryGetValue(componentType, out TableManager tm); return tm;}
        public static TableManager FindTableByMUDTable(IMudTable mudTable) { TableDictionary.TableDict.TryGetValue(mudTable.GetType().Name, out TableManager tm); return tm; }
        public static TableManager FindTableByMUDTable(Type mudTableType) { TableDictionary.TableDict.TryGetValue(mudTableType.Name, out TableManager tm); return tm; }

        public static T MakeTable<T>(string entityKey) where T : IMudTable, new() {
            T table = new T();
            // IMudTable table = (IMudTable)Activator.CreateInstance(component.TableType);
            return IMudTable.MakeTable<T>(entityKey);
        }

        // public static T FindPrefab<T>() where T : MUDComponent, new() { return (T)(FindTable<T>()?.Prefab);}
        // public static MUDComponent FindPrefab(IMudTable table) { return FindTableByMUDTable(table)?.Prefab;}

    }
}