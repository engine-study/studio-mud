using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace mud {

    public class MUDTableObject : ScriptableObject {
        public string TableName { get { return tableName; } }
        public string TableUpdateName { get { return tableUpdateName; } }
        public Type Table { get { return Type.GetType($"{TableName}, mudworld"); } }
        public Type TableUpdate { get { return Type.GetType($"{TableUpdateName}, mudworld"); } }

        [Header("Table")]
        [SerializeField] string tableName;
        [SerializeField] string tableUpdateName;


        #if UNITY_EDITOR
        public void SetTable(Type newtable) {

            MUDTable table = (MUDTable)Activator.CreateInstance(newtable);
            
            tableName = table.TableType().FullName;
            tableUpdateName = table.TableUpdateType().FullName;

        }
        #endif

    }

}
