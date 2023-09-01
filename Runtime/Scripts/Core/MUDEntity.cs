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
        public System.Action OnComponent;
        public System.Action<MUDComponent> OnComponentAdded, OnComponentRemoved;
        public System.Action<MUDComponent, UpdateInfo> OnComponentUpdated;
        public System.Action OnInit, OnUpdated;
        Dictionary<string, MUDComponent> componentDict;


        [Header("MUD")]
        [SerializeField] string mudKey;
        [SerializeField] string entityName;
        [SerializeField] List<MUDComponent> components;
        [SerializeField] List<MUDComponent> expected;

        public void SetName(string newName) {entityName = newName; gameObject.name = entityName;}

        protected override void Awake() {
            base.Awake();

            componentDict = new Dictionary<string, MUDComponent>();
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

            if (HasInit) {
                Debug.LogError("Double init", this);
                return;
            }

            Init();
            OnInit?.Invoke();
        }

        protected async void InitEntityCheck(MUDComponent newComponent) {

            //entities must have at least one component and have loaded all expected components
            if (components.Count < 1 || expected.Count > components.Count) { return; }

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

        //feels like a hack but, we have to use GetType() when the function is passed a component (in this case typeof(T) returns wrong base class (compile-time type))
        //and when the function doesn't have a comonent ex. (GetMudComponent<PositionComponent>), then we can safely use typeof(T);
        public T GetMUDComponent<T>() where T : MUDComponent {
            componentDict.TryGetValue(typeof(T).Name, out MUDComponent value);
            return value as T;
        }
        public T GetMUDComponent<T>(T component) where T : MUDComponent {
            componentDict.TryGetValue(component.GetType().Name, out MUDComponent value);
            return value as T;
        }

        public T AddComponent<T>(T prefab, SpawnInfo newSpawnInfo) where T : MUDComponent {
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
                componentDict.Add(c.GetType().Name, c);

                UpdateExpected(prefab);

                //init it
                c.DoInit(newSpawnInfo);
                c.OnUpdatedInfo += ComponentUpdate;

                OnComponentAdded?.Invoke(c);
                OnComponent?.Invoke();
            }

            return c;
        }

        public void UpdateExpected(MUDComponent componentPrefab) {
            expected.Add(componentPrefab);
            List<MUDComponent> newExpected = expected.Union(componentPrefab.RequiredComponents).ToList();
            expected = newExpected;
        }


        public void RemoveComponent(MUDComponent c) {

            if (c == null) {

            } else {
                c.OnUpdatedInfo -= ComponentUpdate;
                components.Remove(c);
                componentDict.Remove(c.GetType().Name);
                OnComponentRemoved?.Invoke(c);
            }

            OnComponent?.Invoke();

            Destroy(c);
        }


        void ComponentUpdate(MUDComponent c, UpdateInfo i) {
            OnComponentUpdated?.Invoke(c, i);
            OnUpdated?.Invoke();
        }


    }
}