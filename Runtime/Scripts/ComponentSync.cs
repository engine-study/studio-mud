using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace mud.Client
{


    public abstract class ComponentSync : MonoBehaviour
    {
        public enum ComponentSyncType { Instant, Lerp, InitialSyncOnly }

        public System.Action OnSync, OnUpdate;

        [Header("Settings")]
        [SerializeField] protected ComponentSyncType syncType;

        [Header("Debug")]
        [SerializeField] protected MUDComponent componentPrefab;
        [SerializeField] protected MUDComponent ourComponent;
        [SerializeField] protected MUDComponent targetComponent;


        protected abstract string GetComponentName();


        protected virtual void Awake() {

            ourComponent = GetComponent<MUDComponent>();
            componentPrefab = ComponentDictionary.StringToComponentPrefab(GetComponentName());

            if (!ourComponent.RequiredComponents.Contains(componentPrefab))
            {
                Debug.Log("Adding our required component.", gameObject);
                ourComponent.RequiredComponents.Add(componentPrefab);
            }

            ourComponent.OnLoaded += DoSync;
        }

        void OnDestroy()
        {
            if (ourComponent) { ourComponent.OnLoaded -= DoSync; }
            if (targetComponent) { targetComponent.OnUpdated -= DoUpdate; }
        }

        void DoSync() {
            InitComponents();
            InitialSync();
            OnSync?.Invoke();
        }

        void DoUpdate() {
            UpdateSync();
            OnUpdate?.Invoke();
        }

        protected virtual void InitComponents() {

            //get our targetcomponent
            targetComponent = ourComponent.Entity.GetMUDComponent(componentPrefab);

            //if we want to keep lerping towards the value we get, enable this component
            enabled = syncType == ComponentSyncType.Lerp;
            
            //if we want to keep syncing, listen for further updates
            if(syncType != ComponentSyncType.InitialSyncOnly) {
                targetComponent.OnUpdated += DoUpdate;
            }

        }

        //first initial sync with network values
        //UpdateSync would
        protected virtual void InitialSync() {

            //do our first "normal" updatesync update
            UpdateSync();

        }

        //updated sync with new values midway through play
        protected virtual void UpdateSync() {
            //.... override this update the values here
            //ex.             

        }

        protected virtual void Update()
        {
            UpdateLerp();
        }

        protected virtual void UpdateLerp() {

        }
    }

}