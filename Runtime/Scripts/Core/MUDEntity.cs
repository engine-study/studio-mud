using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Cysharp.Threading.Tasks;

namespace mud.Client {
    public class MUDEntity : Entity {
        public string Key { get { return mudKey; } }
        public List<MUDComponent> Components { get { return components; } }
        public List<MUDComponent> ExpectedComponents { get { return expected; } }
        public System.Action<MUDComponent> OnComponentAdded, OnComponentRemoved;
        public System.Action<MUDComponent, UpdateEvent> OnComponentUpdated;
        public System.Action OnInit, OnUpdated;


        [Header("MUD")]
        [SerializeField] protected string mudKey;
        [SerializeField] protected List<MUDComponent> components;
        [SerializeField] protected List<MUDComponent> expected;


        protected virtual void Awake() {
            base.Awake();

            expected = new List<MUDComponent>();
            components = new List<MUDComponent>();

        }

        protected override void Start() {
            base.Start();

        }

        public override void Init() {
            base.Init();

            if (string.IsNullOrEmpty(mudKey)) {
                Debug.LogError("NO entity key");
                return;
            }

        }

        void DoInit() {

            if (hasInit) {
                Debug.LogError("Double init", this);
                return;
            }

            Init();
            gameObject.SetActive(true);
            hasInit = true;
            OnInit?.Invoke();
        }

        protected async void InitEntityCheck(MUDComponent newComponent) {

            if (components.Count < 1 || expected.Count > components.Count) {
                return;
            }

            OnComponentAdded -= InitEntityCheck;
            DoInit();

        }

        protected override void Destroy() {
            base.Destroy();

            for (int i = components.Count - 1; i > -1; i--) {
                RemoveComponent(components[i]);
            }
        }

        public void InitEntity(string newKey) {
            if (!string.IsNullOrEmpty(mudKey)) { Debug.LogError("We already have a key?", this); }
            mudKey = newKey;

            gameObject.SetActive(false);
            OnComponentAdded += InitEntityCheck;
        }


        public MUDComponent GetMUDComponent(MUDComponent component) {
            for (int i = 0; i < Components.Count; i++) { if (Components[i].GetType() == component.GetType()) { return Components[i]; } }
            return null;
        }

        public async UniTask<T> GetMUDComponentAsync<T>(T componentType = null) where T : MUDComponent {
            MUDComponent component = GetMUDComponent<T>();

            int timeout = 100;
            while (component == null) {

                timeout--;
                if (timeout < 0) {
                    Debug.LogError("Timeout: could not find " + typeof(T).ToString(), this);
                    return null;
                }

                await UniTask.Delay(100);
                component = GetMUDComponent<T>();

            }

            return component as T;
        }

        public T GetMUDComponent<T>() where T : MUDComponent {
            for (int i = 0; i < Components.Count; i++) { if (Components[i].GetType() == typeof(T)) { return (T)Components[i]; } }
            return null;
        }
        public MUDComponent AddComponent(MUDComponent componentPrefab, TableManager fromTable) {
            // Debug.Log("Adding " + componentPrefab.gameObject.name, gameObject);
            MUDComponent c = GetMUDComponent(componentPrefab);

            if (c) {
                Debug.LogError(componentPrefab.gameObject.name + " already exists.", gameObject);
            } else {
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
                c.OnUpdatedFull += ComponentUpdate;

                OnComponentAdded?.Invoke(c);
            }

            return c;
        }

        void ComponentUpdate(MUDComponent c, UpdateEvent u) {
            OnComponentUpdated?.Invoke(c, u);
            OnUpdated?.Invoke();
        }

        public void RemoveComponent(MUDComponent c) {

            if (c == null) {
                Debug.LogError("Removing a null component", this);
            }

            c.OnUpdatedFull -= ComponentUpdate;

            components.Remove(c);
            OnComponentRemoved?.Invoke(c);


            Destroy(c);
        }



    }
}