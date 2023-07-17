using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client {

    public abstract class ComponentSync : MonoBehaviour {
        public enum ComponentSyncType { Instant, Lerp, InitialSyncOnly }

        public System.Action OnSync, OnUpdate;

        [Header("Settings")]
        [SerializeField] protected ComponentSyncType syncType;

        [Header("Debug")]
        protected MUDComponent componentPrefab;
        protected MUDComponent ourComponent;
        protected MUDComponent targetComponent;

        string componentString;


        //if we wanted to sync position, we would return the Position component class for example
        public abstract System.Type TargetComponentType();

        protected virtual void Start() {

            //do not let the update loop fire
            enabled = false;

            ourComponent = GetComponent<MUDComponent>();
            componentString = TargetComponentType().ToString();

            if(string.IsNullOrEmpty(componentString)) { Debug.LogError("Could not find component type", this);}

            componentPrefab = ComponentDictionary.StringToComponentPrefab(componentString);

            if (!ourComponent.RequiredComponents.Contains(componentPrefab)) {
                // Debug.Log("Adding our required component.", gameObject);
                ourComponent.RequiredComponents.Add(componentPrefab);
            }

            ourComponent.OnLoaded += DoSync;
        }

        void OnDestroy() {
            if (ourComponent) { ourComponent.OnLoaded -= DoSync; }
            if (targetComponent) { targetComponent.OnUpdatedDetails -= DoUpdate; }
        }

        void DoSync() {
            enabled = true;
            InitComponents();
            InitialSync();
            OnSync?.Invoke();
        }

        void DoUpdate(UpdateEvent updateType) {
            UpdateSync(updateType);
            OnUpdate?.Invoke();
        }

        protected virtual void InitComponents() {

            //get our targetcomponent
            targetComponent = ourComponent.Entity.GetMUDComponent(componentPrefab);

            //if we want to keep lerping towards the value we get, enable this component
            enabled = syncType == ComponentSyncType.Lerp;

            //if we want to keep syncing, listen for further updates
            if (syncType != ComponentSyncType.InitialSyncOnly) {
                targetComponent.OnUpdatedDetails += DoUpdate;
            }

        }

        //first initial sync with network values
        //UpdateSync would
        protected virtual void InitialSync() {

            //do our first "normal" updatesync update
            UpdateSync(UpdateEvent.Insert);

        }

        //updated sync with new values midway through play
        protected virtual void UpdateSync(UpdateEvent updateType) {
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