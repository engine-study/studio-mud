using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Cysharp.Threading.Tasks;

namespace mud.Client {
    public class MUDEntity : MonoBehaviour {

        public bool HasInit{get{return hasInit;}}
        public string Key { get { return mudKey; } }
        public string Name {get{return entityName;}}
        public bool Loaded {get{return loaded;}}
        public List<MUDComponent> Components { get { return components; } }
        public List<Type> ExpectedComponents { get { return expected; } }
        public Action OnComponent;
        public Action<MUDComponent> OnComponentAdded, OnComponentRemoved;
        public Action<MUDComponent, UpdateInfo> OnComponentUpdated;
        public Action OnLoaded, OnUpdated;
        public Action<MUDEntity> OnLoadedInfo, OnUpdatedInfo;
        Dictionary<string, MUDComponent> componentDict;
        Dictionary<Type, MUDComponent> componentTypeDict;


        [Header("MUD")]
        [SerializeField] bool hasInit;
        [SerializeField] bool loaded;
        [SerializeField] string mudKey;
        [SerializeField] string entityName;
        [SerializeField] List<MUDComponent> components;
        [SerializeField] List<MUDComponent> expectedComponents;
        [SerializeField] List<Type> expected = null;

        public void SetName(string newName) {entityName = newName; gameObject.name = entityName;}

        protected virtual void Awake() { }
        protected virtual void Start() { }
        protected virtual void OnDestroy() {if(hasInit) { Destroy();}}


        public void DoInit(string newKey) {

            Init(newKey);
            hasInit = true; 

        }

        public virtual void Init(string newKey) {   
            Debug.Assert(hasInit == false, "Double init", this);

            componentDict = new Dictionary<string, MUDComponent>();
            componentTypeDict = new Dictionary<Type, MUDComponent>();

            expected = new List<Type>();
            expectedComponents = new List<MUDComponent>();
            components = new List<MUDComponent>();

            mudKey = newKey;
        }

        //entities must have at least one component and have loaded all expected components
        async void HandleNewComponent(MUDComponent newComponent) {
            
            //update our required components
            UpdateExpected(newComponent);

            //wait to see if a new component is getting loaded this same frame
            await UniTask.Delay(100);
            
            //check if we have those components
            if (components.Count < 1 || expected.Count > components.Count) { return; }
            
            //when we do, send a message to all components we have loaded
            DoLoad();
            
        }

        //let our added components update the amount of expected components
        public void UpdateExpected(MUDComponent newComponent) {

            //add the component itself to the expected list, and all its required types
            Type newComponentType = newComponent.GetType();
            if(expected.Contains(newComponentType) == false) {
                expected.Add(newComponentType);
                expectedComponents.Add(newComponent);
            }

            for(int i = 0; i < newComponent.RequiredPrefabs.Count; i++) {
                Type t = newComponent.RequiredPrefabs[i].GetType();
                if(expected.Contains(t) == false) {
                    expected.Add(t);
                    expectedComponents.Add(newComponent.RequiredPrefabs[i]);
                }
            }
        }

        void DoLoad() {

            loaded = true;
            OnLoaded?.Invoke();
            OnLoadedInfo?.Invoke(this);
        }

        public void Toggle(bool toggle) {
            gameObject.SetActive(toggle);
            // for(int i = 0; i < components.Count; i++) {
            //     components[i].Toggle(toggle);
            // }
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

                //init it
                c.DoInit(newSpawnInfo);
                c.OnUpdatedInfo += ComponentUpdate;

                OnComponentAdded?.Invoke(c);
                OnComponent?.Invoke();

                HandleNewComponent(c);
            }

            return c;
        }

        void ComponentUpdate(MUDComponent c, UpdateInfo i) {
            OnComponentUpdated?.Invoke(c, i);
            OnUpdated?.Invoke();
            OnUpdatedInfo?.Invoke(this);
        }

        protected virtual void Destroy() {
            for (int i = components.Count - 1; i > -1; i--) { RemoveComponent(components[i]); }
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
        


    }
}