
using System;
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
        public Action OnInit;
        public Action OnAdded, OnUpdated, OnDeleted;
        bool hasInit;

        protected virtual void Awake()
        {

        }
        protected virtual void Start()
        {

            net = mud.Unity.NetworkManager.Instance;

            if(NetworkManager.NetworkInitialized) {
                DoInit(net);
            } else {
                net.OnNetworkInitialized += DoInit;
            }

        }

        protected virtual void OnDestroy()
        {
            _disposers?.Dispose();
            net.OnNetworkInitialized -= DoInit;
        }

        void DoInit(NetworkManager nm) {

            InitTable();

            hasInit = true;
            OnInit?.Invoke();

        }   

        protected virtual async void InitTable()
        {
            if(hasInit) {
                Debug.LogError("Oh no, double Init", this);
                return;
            }

            Subscribe(net);            
            Debug.Log("Init: " + gameObject.name);
        }

        protected abstract void Subscribe(NetworkManager nm);
        protected abstract void OnInsertRecord(RecordUpdate tableUpdate);
        protected abstract void OnDeleteRecord(RecordUpdate tableUpdate);
        protected abstract void OnUpdateRecord(RecordUpdate tableUpdate);



    }
}