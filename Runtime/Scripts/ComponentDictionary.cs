using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client
{
    public class ComponentDictionary : MonoBehaviour
    {
        public static ComponentDictionary Instance;
        private static Dictionary<string, MUDComponent> m_componentPrefabs;

        void Awake()
        {
            Instance = this;
            m_componentPrefabs = new Dictionary<string, MUDComponent>();
        }

        void OnDestroy()
        {
            Instance = null;
        }

        public static MUDComponent StringToComponentPrefab(string componentName)
        {

            MUDComponent prefab = null;

            m_componentPrefabs.TryGetValue(componentName, out prefab);

            if (prefab == null)
            {

                prefab = (Resources.Load("Components/" + componentName) as GameObject).GetComponent<MUDComponent>();

                if (prefab)
                {
                    m_componentPrefabs.Add(componentName, prefab);
                }
                else
                {
                    Debug.LogError("No component of type " + componentName + " in a Resouces/Components folder.");
                    return null;
                }
            }

            return prefab;

        }
    }
}

