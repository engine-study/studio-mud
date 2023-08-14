using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client
{


    public class MUDWorld : MonoBehaviour
    {

        public static T FindComponent<T>(string entity) where T : MUDComponent, new()
        {

            //try to find the tablemanager
            TableManager tm = FindTable<T>();
            tm.Components.TryGetValue(entity, out MUDComponent component);
            return component as T;
        }

        public static T FindOrMakeComponent<T>(string entityKey) where T : MUDComponent, new()
        {

            //try to find the tablemanager
            TableManager tm = FindTable<T>();
            MUDEntity entity = EntityDictionary.FindOrSpawnEntity(entityKey);
            MUDComponent component = entity.AddComponent<T>(ComponentDictionary.FindPrefab<T>(), tm);
            return component as T;
        }


        public static TableManager FindTable<T>() where T : MUDComponent, new()
        {
            MUDComponent refComponent = new T();
            TableDictionary.TableDict.TryGetValue(refComponent.TableName, out TableManager tm);
            if (tm == null) { Debug.LogError("Could not find " + refComponent.TableName + " table"); }
            return tm;
        }

        public static T FindValue<T>(string entityKey) where T : IMudTable, new()
        {
            T table = new T();
            // IMudTable table = (IMudTable)Activator.CreateInstance(component.TableType);
            return IMudTable.GetValueFromTable<T>(entityKey);
        }
    }
}