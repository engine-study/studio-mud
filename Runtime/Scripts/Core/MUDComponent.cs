using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using mud.Client;
using NetworkManager = mud.Unity.NetworkManager;

namespace mud.Client
{


    public abstract class MUDComponent : MonoBehaviour
    {
        public MUDEntity Entity { get { return entity; } }
        public bool Loaded { get { return loaded; } }
        public List<MUDComponent> RequiredComponents { get { return requiredComponents; } }
        public System.Action OnLoaded, OnUpdated;
        public System.Type ComponentToTableType{get{return tableManager.TableType();}}
        public MUDTableManager TableManager {get{return tableManager;}}
        protected IMudTable activeTable;
        protected IMudTable onchainTable;
        protected IMudTable optimisticTable;

        [Header("Settings")]
        [SerializeField] List<MUDComponent> requiredComponents;


        [Header("Debug")]
        protected MUDEntity entity;
        protected MUDTableManager tableManager;
        int componentsLoaded = 0;
        bool hasInit;
        bool loaded = false;


        protected virtual void Awake()
        {

        }

        public virtual void Init(MUDEntity ourEntity, MUDTableManager ourTable)
        {
            entity = ourEntity;
            tableManager = ourTable;

            tableManager.RegisterComponent(true, this);

            LoadComponents();

            hasInit = true;
        }

        protected virtual void Subscribe(NetworkManager nm, GameObject go)
        {

        }


        async UniTaskVoid LoadComponents()
        {

            //always delay a frame so that RequiredComponents has been fully added to by any other scripts on Start and Awake
            await UniTask.Delay(100);

            for (int i = 0; i < requiredComponents.Count; i++)
            {
                await entity.GetMUDComponentAsync(requiredComponents[i]);
                componentsLoaded++;
            }

            loaded = true;
            OnLoaded?.Invoke();

        }

        public virtual void OnDestroy()
        {
            if (hasInit)
            {

            }
        }

        public virtual void Cleanup()
        {
            tableManager.RegisterComponent(false, this);
        }

        public void DoUpdate(mud.Client.IMudTable table, UpdateEvent eventType)
        {
            //update our internal table
            IngestUpdate(table, eventType);

            //use internal table to update component
            UpdateComponent(activeTable, eventType);
            OnUpdated?.Invoke();
        }

        protected virtual void IngestUpdate(mud.Client.IMudTable table, UpdateEvent eventType) {

            //set our onchain toable
            if(eventType == UpdateEvent.Optimistic) {
                optimisticTable = table;
            } else if(eventType != UpdateEvent.Revert) {
                onchainTable = table;
            }
            
            if(eventType == UpdateEvent.Revert) {
                Debug.Assert(onchainTable != null,"Reverting before we have an onchain update");
                optimisticTable = null;
                activeTable = onchainTable;
            } else {
                activeTable = table;
            }
     
        }

        protected virtual void UpdateComponent(mud.Client.IMudTable table, UpdateEvent eventType){}



    }
}