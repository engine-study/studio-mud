using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace mud.Client
{
    public class MUDEntity : Entity
    {
        public string Key { get { return mudKey; } }
        public List<MUDComponent> Components { get { return components; } }
        public System.Action<MUDComponent> OnComponentAdded, OnComponentRemoved;


        [Header("MUD")]
        [SerializeField] protected string mudKey;
        [SerializeField] protected bool isLocal;
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

            if (net.isNetworkInitialized) { InitOnNetwork(net); }
            else { net.OnNetworkInitialized += InitOnNetwork; }

        }


        public override void Init()
        {
            base.Init();

            if (string.IsNullOrEmpty(mudKey))
            {
                Debug.LogError("NO entity key");
                return;
            }

        }

        protected virtual void InitOnNetwork(mud.Unity.NetworkManager nm)
        {
            Init();
        }

        protected override void Destroy()
        {
            base.Destroy();

            net.OnNetworkInitialized -= InitOnNetwork;

            for (int i = components.Count - 1; i > -1; i--)
            {
                RemoveComponent(components[i]);
            }
        }

        public void SetMudKey(string newKey)
        {
            if (!string.IsNullOrEmpty(mudKey)) { Debug.LogError("Can we change mudKeys? Probably not.");}
            mudKey = newKey;
        }

        public void SetIsLocal(bool newValue) { isLocal = newValue; }



        public MUDComponent GetMUDComponent(MUDComponent component)
        {
            for (int i = 0; i < Components.Count; i++) { if (Components[i].GetType() == component.GetType()) { return Components[i]; } }
            return null;
        }

        public async UniTask<MUDComponent> GetMUDComponentAsync(MUDComponent componentType) {
            MUDComponent component = GetMUDComponent(componentType);

            int timeout = 100;
            while (component == null)
            {

                timeout--;
                if (timeout < 0)
                {
                    return null;
                }

                await UniTask.Delay(100);
                component = GetMUDComponent(componentType);

            }

            return component;
        }
        public async UniTask<MUDComponent> GetMUDComponentAsync<T>() {
            MUDComponent component = GetMUDComponent<T>();

            int timeout = 100;
            while (component == null)
            {

                timeout--;
                if (timeout < 0)
                {
                    return null;
                }

                await UniTask.Delay(100);
                component = GetMUDComponent<T>();

            }

            return component;
        }

        public MUDComponent GetMUDComponent<T>()
        {
            for (int i = 0; i < Components.Count; i++) { if (Components[i].GetType() == typeof(T)) { return (Components[i]); } }
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
                OnComponentAdded?.Invoke(c);
            }

            return c;
        }

        public void RemoveComponent(MUDComponent c)
        {

            components.Remove(c);
            OnComponentRemoved?.Invoke(c);

            Destroy(c);
        }



    }
}