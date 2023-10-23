using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud;
using System;

namespace mud
{

    public class TableDictionary : MonoBehaviour
    {
        public static Action<bool, TableManager> OnTableToggle;

        public static TableDictionary Instance;
        public static List<TableManager> Tables {get { return Instance.tables; } }

        public static Dictionary<string, TableManager> TableDict;
        public static Dictionary<Type, TableManager> ComponentManagerDict;
        public static Dictionary<Type, TableManager> TableManagerDict;
        public List<TableManager> tables;

        public static void AddTable(TableManager table) {

            TableDict.Add(table.ComponentName, table);
            ComponentManagerDict.Add(table.Prefab.GetType(), table);
            TableManagerDict.Add(table.Prefab.MUDTableType, table);

            Tables.Add(table);
            OnTableToggle?.Invoke(true, table);
        }

        public static void DeleteTable(TableManager table) {

            if(Instance == null) { return;}
            
            TableDict.Remove(table.ComponentName);
            ComponentManagerDict.Remove(table.Prefab.GetType());
            TableManagerDict.Remove(table.Prefab.MUDTableType);

            Tables.Remove(table);
            OnTableToggle?.Invoke(false, table);
        }

        void Awake() {

            Instance = this;
            TableDict = new Dictionary<string, TableManager>();
            ComponentManagerDict = new Dictionary<Type, TableManager>();
            TableManagerDict = new Dictionary<Type, TableManager>();
            tables = new List<TableManager>();

        }
        
        void OnDestroy() {

            Instance = null;
            TableDict = null;
            ComponentManagerDict = null;
            TableManagerDict = null;
            tables = null;

        }


        

    }
}

