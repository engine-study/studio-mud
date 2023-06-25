using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client
{
    public class MUDEntity : MonoBehaviour
    {
        public string Key { get { return mudKey; } }
        public List<MUDComponent> Components { get { return components; } }
        public static Dictionary<string, MUDEntity> Entities;

        [Header("MUD")]
        [SerializeField] protected string mudKey;
        [SerializeField] protected bool isLocal;

        [Header("Debug")]
        [SerializeField] protected List<MUDComponent> components;

        private mud.Unity.NetworkManager net;


        public void SetMudKey(string newKey) { mudKey = newKey; }
        public void SetIsLocal(bool newValue) { isLocal = newValue; }

        public MUDComponent GetMUDComponent(MUDComponent component)
        {
            for (int i = 0; i < Components.Count; i++) { if (Components[i].GetType() == component.GetType()) { return Components[i]; } }
            return null;
        }
        public MUDComponent GetMUDComponent<T>()
        {
            for (int i = 0; i < Components.Count; i++) { if (Components[i].GetType() == typeof(T)) { return Components[i]; } }
            return null;
        }
        public MUDComponent AddComponent(MUDComponent componentPrefab, MUDTableManager fromTable)
        {
            // Debug.Log("Adding " + componentPrefab.gameObject.name, gameObject);
            MUDComponent c = GetMUDComponent(componentPrefab);

            if(c) {
                // Debug.Log(componentPrefab.gameObject.name + " already exists.", gameObject);
            } else {
                c = Instantiate(componentPrefab, transform.position, Quaternion.identity, transform);
                c.gameObject.name = c.gameObject.name.Replace("(Clone)", "");
                components.Add(c);
                c.Init(this, fromTable);
            }

            return c;

        }
        public void RemoveComponent(MUDComponent component)
        {
            components.Remove(component);
            Destroy(component);
        }


        public static MUDEntity GetEntity(string Key) { return Entities[Key]; }
        public static MUDEntity GetEntitySafe(string Key) { MUDEntity e; Entities.TryGetValue(Key, out e); return e; }
        public static void ToggleEntity(bool toggle, MUDEntity entity)
        {
            if (toggle) { Entities.Add(entity.Key, entity); }
            else { Entities.Remove(entity.Key); }
        }

        protected virtual void Awake()
        {
            if (Entities == null)
            {
                Entities = new Dictionary<string, MUDEntity>();
            }

            components = new List<MUDComponent>();
        }



        protected virtual void Start()
        {
            net = mud.Unity.NetworkManager.Instance;

            if (net.isNetworkInitialized)
            {
                InitOnNetwork(net);
            }
            else
            {
                net.OnNetworkInitialized += InitOnNetwork;
            }

        }

        protected virtual void OnDestroy()
        {
            if (net)
            {
                net.OnNetworkInitialized -= InitOnNetwork;
            }
        }

        protected virtual void InitOnNetwork(mud.Unity.NetworkManager nm)
        {

            // for (int i = 0; i < components.Length; i++)
            // {
            //     //copy the scriptable object (so we don't write to the original)
            //     components[i] = Instantiate(components[i]);
            //     components[i].Init(this);
            // }

            Init();

        }

        public virtual void Init()
        {

        }
    }
}