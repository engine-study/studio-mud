using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mud.Unity;
using UniRx;

namespace mud.Client
{


    public class MUDNetworkSync : MonoBehaviour
    {

        public System.Action OnNetworkInit;
        mud.Unity.NetworkManager net;
        protected CompositeDisposable _disposers = new();

        protected virtual void Awake()
        {

        }

        protected virtual void Start()
        {
            net = mud.Unity.NetworkManager.Instance;
            net.OnNetworkInitialized += DoInit;
        }

        protected virtual void OnDestroy()
        {
            _disposers?.Dispose();
            net.OnNetworkInitialized -= DoInit;
        }

        // var SpawnSubscription = table.OnRecordInsert().ObserveOnMainThread().Subscribe(OnUpdateTable);
        // _disposers.Add(SpawnSubscription);

        // var UpdateSubscription  = ObservableExtensions.Subscribe(PositionTable.OnRecordUpdate().ObserveOnMainThread(),
        //         OnChainPositionUpdate);
        // _disposers.Add(UpdateSubscription);

        void DoInit(mud.Unity.NetworkManager nm) {
            Init(nm);
            OnNetworkInit?.Invoke();
        }
        protected virtual async void Init(mud.Unity.NetworkManager nm)
        {

        }

    }
}