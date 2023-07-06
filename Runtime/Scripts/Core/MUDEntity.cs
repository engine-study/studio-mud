using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace mud.Client
{
    public class MUDEntity : Entity
    {
        public string Key { get { return mudKey; } }
        public List<MUDComponent> Components { get { return components; } }
        public List<MUDComponent> ExpectedComponents {get {return expected;}}
        public System.Action<MUDComponent> OnComponentAdded, OnComponentRemoved;
        public System.Action OnInit;


        [Header("MUD")]
        [SerializeField] protected string mudKey;
        [SerializeField] protected List<MUDComponent> components;
        [SerializeField] protected List<MUDComponent> expected;


        protected virtual void Awake()
        {
            base.Awake();

            expected = new List<MUDComponent>();
            components = new List<MUDComponent>();

        }

        protected override void Start()
        {
            base.Start();

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

        void DoInit() {

            if(hasInit) {
                Debug.LogError("Double init", this);
                return;
            }

            Init();
            gameObject.SetActive(true);
            hasInit = true;
            OnInit?.Invoke();
        }

        protected async void InitEntityCheck(MUDComponent newComponent) {

            if(components.Count < 1 || expected.Count > components.Count) {
                return;
            }

            OnComponentAdded -= InitEntityCheck;
            DoInit();
            
        }

        protected override void Destroy()
        {
            base.Destroy();

            for (int i = components.Count - 1; i > -1; i--)
            {
                RemoveComponent(components[i]);
            }
        }

        public void InitEntity(string newKey)
        {
            if (!string.IsNullOrEmpty(mudKey)) { Debug.LogError("We already have a key?", this);}
            mudKey = newKey;

            gameObject.SetActive(false);
            OnComponentAdded += InitEntityCheck;
        }


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

                await UniTask.Delay(500);
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
                //spawn the compoment
                c = Instantiate(componentPrefab, transform.position, Quaternion.identity, transform);
                c.gameObject.name = c.gameObject.name.Replace("(Clone)", "");
                //helpful to show in inspector that this is the compoment instance, not the prefab
                c.gameObject.name += "(I)"; 

                //add the component to both components list, but also add the "required" components
                components.Add(c);
                expected.Add(componentPrefab);
                List<MUDComponent> newExpected = expected.Union(c.RequiredComponents).ToList();
                expected = newExpected;
            
                //init it
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