using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace mud.Client {
    public class ComponentDictionary : MonoBehaviour {
        public static ComponentDictionary Instance;

        void Awake() {
            Instance = this;
        }

        void OnDestroy() {
            Instance = null;
        }


        // public MUDComponent TypeToPrefab<T>
        public static T FindPrefab<T>() where T : MUDComponent, new() {
            return NameToPrefab((new T()).TableName) as T;
        }

        public static T FindPrefab<T>(T component) where T : MUDComponent {
            return NameToPrefab(component.TableName)  as T;
        }


        private static MUDComponent NameToPrefab(string tableName) {
            TableDictionary.TableDict.TryGetValue(tableName, out TableManager tm);
            return tm?.Prefab;
        }
    }
}

