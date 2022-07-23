using System;
using Frictionless;

namespace App.Core.Services
{
    public interface INetworkService : IService
    {
        bool IsNetworkConnected();
    }

    public class NetworkServiceChangeEvent : ServiceFactory.ListenerForChangeService
    {
        private static NetworkServiceChangeEvent _instance = null;

        public static NetworkServiceChangeEvent Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NetworkServiceChangeEvent();
                }

                return _instance;
            }
        }

        private static System.Type _actualType = typeof(INetworkService);
        public INetworkService CurrentService = null;
        public event System.Action<INetworkService> OnNetworkServiceChanged;

        private NetworkServiceChangeEvent()
        {
            if (_instance != null)
            {
                ServiceFactory.RemoveListenerNonTransientServiceChanged(_instance);
            }

            _instance = this;

            ServiceFactory.AddListenerNonTransientServiceChanged(_instance);
            CurrentService = ServiceFactory.Resolve<INetworkService>();
#if UNITY_EDITOR
            ServiceFactory.OnTotalCleanUp -= CleanUpStatics;
            ServiceFactory.OnTotalCleanUp += CleanUpStatics;
#endif
        }

        private static void CleanUpStatics()
        {
            _instance = null;
        }

        public Type GetTypeOfServiceToCheck()
        {
            return _actualType;
        }

        public void OnChangedActualService(System.Object newService)
        {
            CurrentService = newService as INetworkService;
            OnNetworkServiceChanged?.Invoke(CurrentService);
        }
    }
}