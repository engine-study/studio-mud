using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client
{
    public class MUDEntity : Entity
    {
        public string Key { get { return mudKey; } }
        public List<MUDComponent> Components { get { return components; } }

        [Header("MUD")]
        [SerializeField] protected string mudKey;
        [SerializeField] protected bool isLocal;

        [Header("Debug")]
        [SerializeField] protected List<MUDComponent> components;

        private mud.Unity.NetworkManager net;



        protected virtual void Awake()
        {
            base.Awake();

            components = new List<MUDComponent>();
        }



        protected override void Start()
        {
            base.Start();

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

        public override void Init()
        {
            base.Init();

            if(string.IsNullOrEmpty(mudKey)) {
                Debug.LogError("NO entity key");
                return;
            }

        }

        protected override void Destroy()
        {
            base.Destroy();

            if (net)
            {
                net.OnNetworkInitialized -= InitOnNetwork;
            }

            
        }
        
        public void SetMudKey(string newKey)
        {
            if(!string.IsNullOrEmpty(mudKey)) {
                Debug.LogError("Can we change mudKeys? Probably not.");
            }

            mudKey = newKey;

        }
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

            if (c)
            {
                Debug.LogError(componentPrefab.gameObject.name + " already exists.", gameObject);
            }
            else
            {
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



    }
}