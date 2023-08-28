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
        protected MUDComponent componentPrefab;
        protected MUDComponent ourComponent;

        //if we wanted to sync position, we would return the Position component class for example
        public abstract MUDComponent SyncedComponent();

        protected virtual void Awake() {

            //do not let the update loop fire
            enabled = false;
            ourComponent = GetComponent<MUDComponent>();
            ourComponent.OnComponentAwake += SetupSync;
        
        }

        void SetupSync() {

            componentPrefab = MUDWorld.FindPrefab(SyncedComponent().TableReference);
            if (!ourComponent.RequiredComponents.Contains(componentPrefab)) {
                // Debug.Log("Adding our required component.", gameObject);
                ourComponent.RequiredComponents.Add(componentPrefab);
            }

            ourComponent.OnComponentsLoaded += DoSync;
        }

        protected virtual void OnDestroy() {
            if (ourComponent) { ourComponent.OnComponentsLoaded -= DoSync; }
            if (targetComponent) { targetComponent.OnUpdated -= DoUpdate; }
        }

        void DoSync() {

            enabled = true;

            InitComponents();

            InitialSync();
            UpdateSync();

            synced = true;

            OnAwake?.Invoke();
        }

        void DoUpdate() {
            UpdateSync();
            OnUpdate?.Invoke();
        }

        protected virtual void InitComponents() {

            //get our targetcomponent
            targetComponent = ourComponent.Entity.GetMUDComponent(componentPrefab);

            if(targetComponent == null) {
                Debug.LogError("Couldn't find " + componentPrefab.TableName + " to sync.", this);
            }
            
            //if we want to keep lerping towards the value we get, enable this component
            enabled = syncType == ComponentSyncType.Lerp;

            //listen for further updates
            targetComponent.OnUpdated += DoUpdate;
            

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