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

        public static Dictionary<Type, TableManager> TableDict;
        public static Dictionary<Type, TableManager> ComponentManagerDict;
        public List<TableManager> tables;

        public static void AddTable(TableManager table) {

            TableDict.Add(table.Prefab.MUDTableType, table);
            ComponentManagerDict.Add(table.Prefab.GetType(), table);

            Tables.Add(table);
            OnTableToggle?.Invoke(true, table);
        }

        public static void DeleteTable(TableManager table) {

            if(Instance == null) { return;}
            
            TableDict.Remove(table.Prefab.MUDTableType);
            ComponentManagerDict.Remove(table.Prefab.GetType());

            Tables.Remove(table);
            OnTableToggle?.Invoke(false, table);
        }

        void Awake() {

            Instance = this;
            TableDict = new Dictionary<Type, TableManager>();
            ComponentManagerDict = new Dictionary<Type, TableManager>();
            tables = new List<TableManager>();

        }
        
        void OnDestroy() {

            Instance = null;
            TableDict = null;
            ComponentManagerDict = null;
            tables = null;

        }


        

    }
}

