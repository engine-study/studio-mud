using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

namespace mud.Client {

    public abstract class ComponentSync : MonoBehaviour {
        public enum ComponentSyncType { Lerp, Instant }

        public MUDComponent SyncComponent {get{return targetComponent;}}
        public Action OnAwake, OnUpdate, OnDelete;

        [Header("Settings")]
        [SerializeField] protected ComponentSyncType syncType = ComponentSyncType.Instant;

        [Header("Debug")]
        [SerializeField] bool synced;
        [SerializeField] bool started;
        [SerializeField] MUDComponent targetComponent;
        protected TableManager ourTable;
        protected MUDComponent ourComponent;

        //if we wanted to sync position, we would return the Position component class for example
        public abstract Type MUDComponentType();

        public void SetSyncType(ComponentSyncType newType) {
            syncType = newType;
        }

        protected virtual async void Awake() {

            //setup our component
            ourComponent = GetComponent<MUDComponent>();
            if(ourComponent.HasInit) {AddRequiredComponents();}
            else {ourComponent.OnInit += AddRequiredComponents;}
        }

        protected virtual void Start() {
            
            //disable this for updating (for now)
            enabled = false;

            //sync after Awake so that if we use AddComponent we can chance the SyncType before the first sync
            if(ourComponent.Loaded) {DoSync();}
            else {ourComponent.OnLoaded += DoSync;}
        }

         protected virtual void OnDestroy() {
            if (ourComponent) { ourComponent.OnLoaded -= DoSync; ourComponent.OnInit -= AddRequiredComponents;}
            if (targetComponent) { targetComponent.OnUpdated -= DoUpdate; }
        }


        void AddRequiredComponents() {

            //add our required components 
            ourTable = MUDWorld.FindTable(MUDComponentType());
            if(ourTable == null) {Debug.LogError("Could not find table " + MUDComponentType().Name); return;}
            if (!ourComponent.RequiredTypes.Contains(ourTable.Prefab.GetType())) {
                // Debug.Log("Adding our required component.", gameObject);
                ourComponent.RequiredTypes.Add(ourTable.Prefab.GetType());
            }
            
        }

        void DoSync() {

            InitComponents();
            InitialSync();

            synced = true;
            OnAwake?.Invoke();

            //listen for further updates
            targetComponent.OnUpdated += DoUpdate;

        }

        protected virtual void InitComponents() {

            //get our targetcomponent
            targetComponent = ourComponent.Entity.GetMUDComponent(MUDComponentType());
            if(targetComponent == null) { Debug.LogError("Couldn't find " + MUDComponentType() + " to sync.", this);}

        }

        void DoUpdate() {
            Debug.Assert(synced, gameObject.name + " updated before sync.", this);
            
            HandleUpdate();
            OnUpdate?.Invoke();
        }

        //first initial sync with network values
        //UpdateSync would
        protected virtual void InitialSync() {

        }

        //updated sync with new values midway through play
        protected virtual void HandleUpdate() {
            //.... override this update the values here
            //ex.             
        }

        protected virtual void Update() {
            //need this check because Update was being called
            //despite enabled = false
            if(!synced) return;
            UpdateLerp();
        }

        protected virtual void UpdateLerp() {

        }
    }

}