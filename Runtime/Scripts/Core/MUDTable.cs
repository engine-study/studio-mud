using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Unity;
using mud.Client;
using NetworkManager = mud.Unity.NetworkManager;
using UniRx;
using ObservableExtensions = UniRx.ObservableExtensions;
using System.Threading.Tasks;

namespace mud.Client
{


    public abstract class MUDTable : MonoBehaviour
    {

        protected CompositeDisposable _disposers = new();
        protected mud.Unity.NetworkManager net;
        public System.Action OnAdded, OnUpdated, OnDeleted;
        bool hasInit;

        protected virtual void Awake()
        {

        }
        protected virtual void Start()
        {
            net = mud.Unity.NetworkManager.Instance;
            net.OnNetworkInitialized += InitTable;
        }

        protected virtual void OnDestroy()
        {
            _disposers?.Dispose();
            net.OnNetworkInitialized -= InitTable;
        }

        // var SpawnSubscription = table.OnRecordInsert().ObserveOnMainThread().Subscribe(OnUpdateTable);
        // _disposers.Add(SpawnSubscription);

        // var UpdateSubscription  = ObservableExtensions.Subscribe(PositionTable.OnRecordUpdate().ObserveOnMainThread(),
        //         OnChainPositionUpdate);
        // _disposers.Add(UpdateSubscription);

        protected virtual async void InitTable(NetworkManager nm)
        {
            if(hasInit) {
                Debug.LogError("Oh no, double Init", this);
                return;
            }
            
            Subscribe(nm);
            Debug.Log("Init: " + gameObject.name);

            hasInit = true;
        }

        protected abstract void Subscribe(NetworkManager nm);

        protected virtual void OnInsertRecord(RecordUpdate tableUpdate)
        {

        }

        protected virtual void OnDeleteRecord(RecordUpdate tableUpdate)
        {

        }

        protected virtual void OnUpdateRecord(RecordUpdate tableUpdate)
        {

        }



    }
}