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
        public List<Type> ExpectedComponents { get { return expected; } }
        public Action OnComponent;
        public Action<MUDComponent> OnComponentAdded, OnComponentRemoved;
        public Action<MUDComponent, UpdateInfo> OnComponentUpdated;
        public Action OnInit, OnUpdated;
        public Action<MUDEntity> OnInitInfo, OnUpdatedInfo;
        Dictionary<string, MUDComponent> componentDict;
        Dictionary<Type, MUDComponent> componentTypeDict;


        [Header("MUD")]
        [SerializeField] string mudKey;
        [SerializeField] string entityName;
        [SerializeField] List<MUDComponent> components;
        [SerializeField] List<Type> expected;

        public void SetName(string newName) {entityName = newName; gameObject.name = entityName;}

        public void InitEntity(string newKey) {
            if (!string.IsNullOrEmpty(mudKey)) { Debug.LogError("We already have a key?", this); }
            mudKey = newKey;

            componentDict = new Dictionary<string, MUDComponent>();
            componentTypeDict = new Dictionary<Type, MUDComponent>();

            expected = new List<Type>();
            components = new List<MUDComponent>();
        }

        //entities must have at least one component and have loaded all expected components
        void HandleNewComponent(MUDComponent newComponent) {

            if(HasInit) {
                
            } else {
                if (components.Count < 1 || expected.Count > components.Count) { return; }
                DoInit();
            }
        }

        //let our added components update the amount of expected components
        public void UpdateExpected(MUDComponent componentPrefab) {

            //add the component itself to the expected list, and all its required types
            Type newComponentType = componentPrefab.GetType();
            if(expected.Contains(newComponentType) == false) {expected.Add(newComponentType);}

            for(int i = 0; i < componentPrefab.RequiredPrefabs.Count; i++) {
                Type t = componentPrefab.RequiredPrefabs[i].GetType();
                if(expected.Contains(t)) continue;
                expected.Add(t);
            }
        }

        void DoInit() {

            if (HasInit) {
                Debug.LogError("Double init", this);
                return;
            }

            Init();

            OnInit?.Invoke();
            OnInitInfo?.Invoke(this);
        }


        public override void Init() {
            base.Init();

            if (string.IsNullOrEmpty(mudKey)) {
                Debug.LogError("NO entity key");
                return;
            }
        }

        protected override void Destroy() {
            base.Destroy();

            for (int i = components.Count - 1; i > -1; i--) {
                RemoveComponent(components[i]);
            }
        }


        //feels like a hack but, we have to use GetType() when the function is passed a component (in this case typeof(T) returns wrong base class (compile-time type))
        //and when the function doesn't have a comonent ex. (GetMudComponent<PositionComponent>), then we can safely use typeof(T);
        public T GetMUDComponent<T>() where T : MUDComponent {
            componentTypeDict.TryGetValue(typeof(T), out MUDComponent value);
            return value as T;
        }

        public MUDComponent GetMUDComponent(Type componentType) {
            componentTypeDict.TryGetValue(componentType, out MUDComponent value);
            return value;
        }
        
        public T GetMUDComponent<T>(T component) where T : MUDComponent {
            componentDict.TryGetValue(component.MUDTableName, out MUDComponent value);
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
                componentDict.Add(c.MUDTableName, c);
                componentTypeDict.Add(c.GetType(), c);

                UpdateExpected(prefab);

                //init it
                c.DoInit(newSpawnInfo);
                c.OnUpdatedInfo += ComponentUpdate;

                OnComponentAdded?.Invoke(c);
                OnComponent?.Invoke();

                HandleNewComponent(c);
            }

            return c;
        }

        public void RemoveComponent(MUDComponent c) {

            if (c == null) {

            } else {
                c.OnUpdatedInfo -= ComponentUpdate;
                components.Remove(c);
                componentDict.Remove(c.GetType().Name);
                componentTypeDict.Remove(c.GetType());
                OnComponentRemoved?.Invoke(c);
            }

            OnComponent?.Invoke();

            Destroy(c);
        }


        void ComponentUpdate(MUDComponent c, UpdateInfo i) {
            OnComponentUpdated?.Invoke(c, i);
            OnUpdated?.Invoke();
            OnUpdatedInfo?.Invoke(this);
        }


    }
}