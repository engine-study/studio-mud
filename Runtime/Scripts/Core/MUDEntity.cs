using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Cysharp.Threading.Tasks;

namespace mud {
    public class MUDEntity : MonoBehaviour {

        public bool HasInit{get{return hasInit;}}
        public string Key { get { return mudKey; } }
        public string Name {get{return entityName;}}
        public bool Loaded {get{return loaded;}}
        public bool IsLocal {get{return isLocal;}}
        public List<MUDComponent> Components { get { return components; } }
        public List<Type> ExpectedComponents { get { return expected; } }
        public Action OnComponent;
        public Action<MUDComponent> OnComponentAdded, OnComponentRemoved;
        public Action<MUDComponent, UpdateInfo> OnComponentUpdated;
        public Action OnLoaded, OnUpdated;
        public Action<MUDEntity> OnLoadedInfo, OnUpdatedInfo;
        Dictionary<string, MUDComponent> componentDict;
        Dictionary<Type, MUDComponent> componentTypeDict;
        Dictionary<Type, MUDComponent> tableTypeDict;


        [Header("MUD")]
        [SerializeField] bool hasInit;
        [SerializeField] bool loaded;
        [SerializeField] bool isLocal;
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
            
            if(hasInit) {Debug.LogError("Double init", this); return;}

            Init(newKey);
            hasInit = true; 

        }

        protected virtual void Init(string newKey) {   
            Debug.Assert(hasInit == false, "Double init", this);

            componentDict = new Dictionary<string, MUDComponent>();
            componentTypeDict = new Dictionary<Type, MUDComponent>();
            tableTypeDict = new Dictionary<Type, MUDComponent>();

            expected = new List<Type>();
            expectedComponents = new List<MUDComponent>();
            components = new List<MUDComponent>();

            mudKey = newKey;
            isLocal = mudKey == NetworkManager.LocalKey;
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
        public T GetMUDComponent<T>() where T : MUDComponent { componentTypeDict.TryGetValue(typeof(T), out MUDComponent value); return value as T; }
        public MUDComponent GetMUDComponent(Type componentType) { componentTypeDict.TryGetValue(componentType, out MUDComponent value); return value; }
        public C GetMUDComponentByTable<T,C>() where T : IMudTable where C : MUDComponent { tableTypeDict.TryGetValue(typeof(T), out MUDComponent value); return (C)value; }
        public MUDComponent GetMUDComponentByTable<T>() where T : IMudTable { tableTypeDict.TryGetValue(typeof(T), out MUDComponent value); return value;}
        public MUDComponent GetMUDComponentByTable(Type componentType) { tableTypeDict.TryGetValue(componentType, out MUDComponent value); return value;}

        //way of doing a GetComponent on just the roots of the Entity's components
        //quicker than searching everything with a GetComponentInChildren
        public T GetRootComponent<T>() where T : Component {
            for(int i = 0; i < components.Count; i++) {
                T c = components[i].GetComponent<T>();
                if(c) { return c; }
            }
            return null;
        }

        public T AddComponent<T>(T prefab, SpawnInfo newSpawnInfo) where T : MUDComponent {

            if(!hasInit) {Debug.LogError("Not init", this); return null;}
            if(prefab == null) {Debug.LogError("No prefab", this); return null;}

            // Debug.Log("Adding " + componentPrefab.gameObject.name, gameObject);
            T c = (T)GetMUDComponent(prefab.GetType());
      
            if (c == null) {
                //spawn the compoment
                c = Instantiate(prefab, transform.position, Quaternion.identity, transform);
                c.gameObject.name = c.gameObject.name.Replace("(Clone)", "");
                //helpful to show in inspector that this is the compoment instance, not the prefab
                c.gameObject.name += "(I)";

                //add the component to both components list, but also add the "required" components
                components.Add(c);
                componentDict.Add(c.MUDTableName, c);
                componentTypeDict.Add(c.GetType(), c);
                tableTypeDict.Add(c.MUDTableType, c);

                //init it
                c.DoInit(newSpawnInfo);
                c.OnUpdatedInfo += ComponentUpdate;

                OnComponentAdded?.Invoke(c);
                OnComponent?.Invoke();

                HandleNewComponent(c);
            } else {
                // Debug.LogError(prefab.gameObject.name + " already exists.", gameObject);
            }

            return c;
        }

        void ComponentUpdate(MUDComponent c, UpdateInfo i) {
            OnComponentUpdated?.Invoke(c, i);
            OnUpdated?.Invoke();
            OnUpdatedInfo?.Invoke(this);
        }

        protected virtual void Destroy() {
            // for (int i = components.Count - 1; i > -1; i--) { RemoveComponent(components[i]); }
        }

        public void RemoveComponent(MUDComponent c) {

            if (c == null) {

            } else {

                c.OnUpdatedInfo -= ComponentUpdate;
                
                components.Remove(c);
                componentDict.Remove(c.GetType().Name);
                componentTypeDict.Remove(c.GetType());
                tableTypeDict.Remove(c.MUDTableType);

                OnComponentRemoved?.Invoke(c);

                c.DoRelease();

            }

            OnComponent?.Invoke();


        }
        


    }
}