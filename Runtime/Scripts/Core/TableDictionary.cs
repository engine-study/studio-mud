using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Unity;
using System;

namespace mud.Client
{

    public class TableDictionary : MonoBehaviour
    {
        public static Action<bool, TableManager> OnTableToggle;

        public static TableDictionary Instance;
        public static List<TableManager> Tables {get { return Instance.tables; } }

        public static Dictionary<string, TableManager> TableDict;
        public static Dictionary<Type, TableManager> ComponentDict;
        public List<TableManager> tables;

        public static void AddTable(TableManager table) {

            TableDict.Add(table.ComponentName, table);
            ComponentDict.Add(table.Prefab.GetType(), table);

            Tables.Add(table);
            OnTableToggle?.Invoke(true, table);
        }

        public static void DeleteTable(TableManager table) {

            if(Instance == null) {
                return;
            }
            

            TableDict.Remove(table.ComponentName);
            ComponentDict.Remove(table.Prefab.GetType());
            Tables.Remove(table);
            OnTableToggle?.Invoke(false, table);
        }

        void Awake() {

            Instance = this;
            TableDict = new Dictionary<string, TableManager>();
            ComponentDict = new Dictionary<Type, TableManager>();
            tables = new List<TableManager>();

        }
        
        void OnDestroy() {

            Instance = null;
            TableDict = null;
            ComponentDict = null;
            tables = null;

        }


        

    }
}

