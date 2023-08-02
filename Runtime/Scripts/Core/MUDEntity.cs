using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Cysharp.Threading.Tasks;

namespace mud.Client {
    public class MUDEntity : Entity {
        public string Key { get { return mudKey; } }
        public string Name {get{return entityName;}}
        public List<MUDComponent> Components { get { return components; } }
        public List<MUDComponent> ExpectedComponents { get { return expected; } }
        public System.Action<MUDComponent> OnComponentAdded, OnComponentRemoved;
        public System.Action<MUDComponent, UpdateInfo> OnComponentUpdated;
        public System.Action OnInit, OnUpdated;


        [Header("MUD")]
        [SerializeField] protected string mudKey;
        [SerializeField] protected string entityName;
        [SerializeField] protected List<MUDComponent> components;
        [SerializeField] protected List<MUDComponent> expected;
        public void SetName(string newName) {entityName = newName; gameObject.name = entityName;}

        protected override void Awake() {
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
            OnComponentAdded += InitEntityCheck;
        }


        public async UniTask<T> GetMUDComponentAsync<T>(T component = null) where T : MUDComponent {

            T getComponent = GetMUDComponent<T>(component);

            int timeout = 10;
            while (getComponent == null) {

                timeout--;
                if (timeout < 0) { return null; }

                await UniTask.Delay(500);
                getComponent = GetMUDComponent<T>(component);
            }

            return getComponent;
        }

        //TODO find a better solution than a loop to find the component
        //feels like a hack but, we have to use GetType() when the function is passed a component (in this case typeof(T) returns wrong base class (compile-time type))
        //and when the function doesn't have a comonent ex. (GetMudComponent<PositionComponent>), then we can safely use typeof(T);
        public T GetMUDComponent<T>() where T : MUDComponent {
            for (int i = 0; i < Components.Count; i++) { if (Components[i].GetType() == typeof(T)) { return Components[i] as T; }}
            return null;
        }
        
        //TODO find a better solution than a loop to find the component
        public T GetMUDComponent<T>(T component) where T : MUDComponent {
            for (int i = 0; i < Components.Count; i++) { if (Components[i].GetType() == component.GetType()) { return Components[i] as T; } }
            return null;
        }

        public T AddComponent<T>(T prefab, TableManager fromTable) where T : MUDComponent {
            // Debug.Log("Adding " + componentPrefab.gameObject.name, gameObject);
            T c = GetMUDComponent(prefab);

            if (c) {
                // Debug.LogError(prefab.gameObject.name + " already exists.", gameObject);
            } else {
                //spawn the compoment
                c = Instantiate(prefab, transform.position, Quaternion.identity, transform);
                c.gameObject.name = c.gameObject.name.Replace("(Clone)", "");
                //helpful to show in inspector that this is the compoment instance, not the prefab
                c.gameObject.name += "(I)";

                //add the component to both components list, but also add the "required" components
                components.Add(c);
                expected.Add(prefab);
                List<MUDComponent> newExpected = expected.Union(c.RequiredComponents).ToList();
                expected = newExpected;

                //init it
                c.DoInit(this, fromTable);
                c.OnUpdatedInfo += ComponentUpdate;

                OnComponentAdded?.Invoke(c);
            }

            return c;
        }


        public void RemoveComponent(MUDComponent c) {

            if (c == null) {
                Debug.LogError("Removing a null component", this);
            }

            c.OnUpdatedInfo -= ComponentUpdate;

            components.Remove(c);
            OnComponentRemoved?.Invoke(c);

            Destroy(c);
        }


        void ComponentUpdate(MUDComponent c, UpdateInfo i) {
            OnComponentUpdated?.Invoke(c, i);
            OnUpdated?.Invoke();
        }


    }
}