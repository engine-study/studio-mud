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
        [SerializeField] protected ComponentSyncType syncType = ComponentSyncType.Lerp;

        [Header("Debug")]
        [SerializeField] bool synced;
        [SerializeField] MUDComponent targetComponent;
        protected TableManager ourTable;
        protected MUDComponent ourComponent;

        //if we wanted to sync position, we would return the Position component class for example
        public abstract Type MUDComponentType();

        protected virtual void Awake() {

            //do not let the update loop fire
            enabled = false;
            ourComponent = GetComponent<MUDComponent>();

            if(ourComponent.HasInit) {SetupSync();}
            else {ourComponent.OnInit += SetupSync;}
        
        }

        void SetupSync() {

            ourTable = MUDWorld.FindTable(MUDComponentType());
            
            if(ourTable == null) {Debug.LogError("Could not find table " + MUDComponentType().Name); return;}
            if (!ourComponent.RequiredComponents.Contains(ourTable.Prefab)) {
                // Debug.Log("Adding our required component.", gameObject);
                ourComponent.RequiredComponents.Add(ourTable.Prefab);
            }

            if(ourComponent.Loaded) {DoSync();}
            else {ourComponent.OnLoaded += DoSync;}
        }

        protected virtual void OnDestroy() {
            if (ourComponent) { ourComponent.OnLoaded -= DoSync; ourComponent.OnInit -= SetupSync;}
            if (targetComponent) { targetComponent.OnUpdated -= DoUpdate; }
        }

        void DoSync() {

            InitComponents();
            InitialSync();

            synced = true;
            OnAwake?.Invoke();
        }

        protected virtual void InitComponents() {
            //get our targetcomponent
            targetComponent = ourComponent.Entity.GetMUDComponent(MUDComponentType());
            if(targetComponent == null) { Debug.LogError("Couldn't find " + MUDComponentType() + " to sync.", this);}

            //listen for further updates
            targetComponent.OnUpdated += DoUpdate;
        }

        void DoUpdate() {
            if(!synced) {Debug.LogError(gameObject.name + " updated before sync.");}
            UpdateSync();
            OnUpdate?.Invoke();
        }

        //first initial sync with network values
        //UpdateSync would
        protected virtual void InitialSync() {

        }

        //updated sync with new values midway through play
        protected virtual void UpdateSync() {
            //.... override this update the values here
            //ex.             
        }

        protected virtual void Update() {
            UpdateLerp();
        }

        protected virtual void UpdateLerp() {

        }
    }

}