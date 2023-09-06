using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client
{


    public class MUDWorld : MonoBehaviour
    {

        public static T FindComponent<T>(string entity) where T : MUDComponent, new() {
            //try to find the tablemanager
            TableManager tm = FindTable<T>();
            MUDComponent component = null;
            tm?.Components.TryGetValue(entity, out component);
            return (T)component;
        }

        public static T FindOrMakeComponent<T>(MUDEntity entity) where T : MUDComponent, new() { return FindOrMakeComponent<T>(entity.Key); }
        public static T FindOrMakeComponent<T>(string entityKey) where T : MUDComponent, new() {

            //try to find the tablemanager
            TableManager tm = FindTable<T>();
            MUDEntity entity = EntityDictionary.FindOrSpawnEntity(entityKey);
            SpawnInfo si = new SpawnInfo(entity, SpawnSource.InGame, tm);
            MUDComponent component = entity.AddComponent<T>(FindPrefab<T>(), si);
            return component as T;
        }


        public static TableManager FindTable<T>() where T : MUDComponent, new() {
            //TODO we can't creat MUDComponent with new keyword
            MUDComponent refComponent = new T();
            TableDictionary.TableDict.TryGetValue(refComponent.TableName, out TableManager tm);
            return tm;
        }

        public static TableManager FindTable<T>(T c) where T : MUDComponent, new() {
            MUDComponent refComponent = new T();
            TableDictionary.TableDict.TryGetValue(refComponent.TableName, out TableManager tm);
            return tm;
        }

        public static TableManager FindTableFromTable<T>(T c) where T : IMudTable {
            TableDictionary.TableDict.TryGetValue(c.GetType().Name, out TableManager tm);
            return tm;
        }

        public static T FindValue<T>(string entityKey) where T : IMudTable, new() {
            T table = new T();
            // IMudTable table = (IMudTable)Activator.CreateInstance(component.TableType);
            return IMudTable.GetValueFromTable<T>(entityKey);
        }

        
        public static T FindPrefab<T>() where T : MUDComponent, new() {
            return (T)FindTable<T>()?.Prefab;
        }

        public static MUDComponent FindPrefab(IMudTable c) {
            return FindTableFromTable(c)?.Prefab;
        }

    }
}