using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

namespace mud {

    public abstract class ComponentSync : MonoBehaviour {
        public enum ComponentSyncType { Lerp, Instant }

        public MUDComponent SyncComponent {get{return targetComponent;}}
        public Action OnAwake, OnUpdate, OnDelete;

        [Header("Settings")]
        [SerializeField] protected ComponentSyncType syncType = ComponentSyncType.Instant;
        private bool debug = false;

        [Header("Debug")]
        [SerializeField] bool synced;
        [SerializeField] MUDComponent targetComponent;
        protected TableManager ourTable;
        protected MUDComponent ourComponent;

        //if we wanted to sync position, we would return the Position component class for example
        public abstract Type TableType();

        public void SetSyncType(ComponentSyncType newType) {
            syncType = newType;
        }

        protected virtual void Awake() {

            if(debug) {Debug.Log(gameObject.name + " Awake", this);}

            //setup our component
            ourComponent = GetComponent<MUDComponent>();

            if(ourComponent.HasInit) {Init();}
            else {ourComponent.OnInit += Init;}

            if(ourComponent.Loaded) {DoSync();}
            else {ourComponent.OnLoaded += DoSync;}

            //disable this for updating (for now)
            enabled = false;

        }

        protected virtual void Start() {
            
            if(debug) {Debug.Log(gameObject.name + " Start", this);}
      
        }

         protected virtual void OnDestroy() {
            if (ourComponent) { ourComponent.OnLoaded -= DoSync; ourComponent.OnInit -= Init;}
            if (targetComponent) { targetComponent.OnUpdated -= DoUpdate; }
        }


        void Init() {

            if(debug) {Debug.Log(gameObject.name + " Add components", this);}

            //add our required components 
            ourTable = MUDWorld.GetManager(TableType());
            if(ourTable == null) {Debug.LogError("Could not find table " + TableType().Name); return;}
            ourComponent.ToggleRequiredComponent(true, ourTable.Prefab);
            
        }

        void DoSync() {

            if(debug) {Debug.Log(gameObject.name + " Do sync", this);}

            InitComponents();
            InitialSync();

            synced = true;
            OnAwake?.Invoke();

            //listen for further updates
            targetComponent.OnUpdated += DoUpdate;

        }

        protected virtual void InitComponents() {

            //get our targetcomponent
            targetComponent = ourComponent.Entity.GetMUDComponentByTable(TableType());
            if(targetComponent == null) { Debug.LogError("Couldn't find " + TableType() + " to sync.", this);}

        }

        void DoUpdate() {

            if(debug) {Debug.Log(gameObject.name + " Do update", this);}
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
            //need this check because Update was being called despite enabled = false
            UpdateLerp();
        }

        protected virtual void UpdateLerp() {

        }
    }

}