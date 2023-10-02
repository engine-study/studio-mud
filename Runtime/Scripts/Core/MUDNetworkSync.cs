using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UniRx;

namespace mud
{


    public class MUDNetworkSync : MonoBehaviour
    {

        public System.Action OnNetworkInit;
        mud.NetworkManager net;
        protected CompositeDisposable _disposers = new();

        protected virtual void Awake()
        {

        }

        protected virtual void Start()
        {
            net = mud.NetworkManager.Instance;
            net.OnNetworkInitialized += DoInit;
        }

        protected virtual void OnDestroy()
        {
            _disposers?.Dispose();
            net.OnNetworkInitialized -= DoInit;
        }

        void DoInit(mud.NetworkManager nm) {
            Init(nm);
            OnNetworkInit?.Invoke();
        }
        protected virtual async void Init(mud.NetworkManager nm)
        {

        }

    }
}