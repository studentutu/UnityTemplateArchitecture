using System;
using System.Collections.Generic;
using App.Core.CommonPatterns;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace Frictionless
{
    /// <summary>
    /// Must be present in Resources/Managers/any.file
    /// If needed Tests, Create Test ScriptableObject/MOnoBehaviour and inherit from IServiceLocatorConfigureTest
    /// Alternatively - RegisterSingleton Types without providing the instance.
    /// </summary>
    public interface IServiceLocatorConfigure
    {
        System.Type GetConfigurator();
    }

    /// <summary>
    /// Must be present in Resources/Managers/any.file
    /// If needed Tests, Create Test ScriptableObject/MOnoBehaviour and inherit from IServiceLocatorConfigureTest
    /// Alternatively - RegisterSingleton Types without providing the instance.
    /// </summary>
    public interface IServiceLocatorConfigureTest : IServiceLocatorConfigure
    {
    }

    public class SampleServiceLocatorConfiguration : IServiceLocatorConfigure
    {
        public Type GetConfigurator()
        {
            return typeof(SampleServiceLocatorConfiguration);
        }
    }

    /// <summary>
    /// A simple, *single-threaded*, service locator appropriate for use with Unity.
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public static class ServiceFactory
    {
        /// <summary>
        /// Used to listen for changes on the Non-transient singletons
        /// </summary>
        public interface ListenerForChangeService
        {
            System.Type GetTypeOfServiceToCheck();

            void OnChangedActualService(System.Object newService);
        }

        /// <summary>
        /// Mapping of types to concrete type for single instances
        /// </summary>
        private static readonly Dictionary<Type, Type> _singletonMaps = new Dictionary<Type, Type>();

        /// <summary>
        /// Mapping of types to concrete type. Each instance is unique
        /// </summary>
        private static readonly Dictionary<Type, Type> _transientMaps = new Dictionary<Type, Type>();

        /// <summary>
        /// Storage of concrete single instances
        /// </summary>
        private static readonly Dictionary<Type, System.Object> _singletonInstances =
            new Dictionary<Type, System.Object>();

        private static readonly Type _typeOfMainLocatorInterface = typeof(IServiceLocatorConfigure);
        private static bool _isQuiting = false;

        public static event System.Action OnTotalCleanUp
        {
            add { _onTotalCleanUp += value; }
            remove { _onTotalCleanUp -= value; }
        }

        private static System.Action _onTotalCleanUp = NonAllocatedCleanUpLambda;

        private static void NonAllocatedCleanUpLambda()
        {
        }

        public static void AddListenerNonTransientServiceChanged(ListenerForChangeService listener)
        {
            if (listener == null)
            {
                return;
            }

            var typeKey = listener.GetTypeOfServiceToCheck();
            if (typeKey == null)
            {
                Debug.LogError("GetTypeOfServiceToCheck should always return the Type of Abstract Service");
                return;
            }

            if (!_actualNonTransientChangedEvent.ContainsKey(typeKey))
            {
                _actualNonTransientChangedEvent.Add(typeKey, new List<ListenerForChangeService>());
            }

            _actualNonTransientChangedEvent[typeKey].Add(listener);
        }

        public static void RemoveListenerNonTransientServiceChanged(ListenerForChangeService listener)
        {
            if (listener == null)
            {
                return;
            }

            var typeKey = listener.GetTypeOfServiceToCheck();
            if (typeKey == null)
            {
                Debug.LogError("GetTypeOfServiceToCheck should always return the Type of Abstract Service");
                return;
            }

            if (_actualNonTransientChangedEvent.TryGetValue(typeKey, out var listOfListeners) &&
                listOfListeners != null)
            {
                listOfListeners.Remove(listener);
            }
        }

        private static void InvokeNonTransientServiceChange(System.Type typeTarget, System.Object actualService)
        {
            if (_actualNonTransientChangedEvent.TryGetValue(typeTarget, out var listOfListeners) &&
                listOfListeners != null)
            {
                for (int i = 0; i < listOfListeners.Count; i++)
                {
                    if (listOfListeners[i] == null)
                    {
                        continue;
                    }

                    try
                    {
                        listOfListeners[i].OnChangedActualService(actualService);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }


        private static Dictionary<Type, List<ListenerForChangeService>> _actualNonTransientChangedEvent =
            new Dictionary<Type, List<ListenerForChangeService>>();

#if UNITY_EDITOR
        static ServiceFactory()
        {
            void OnChange(PlayModeStateChange change)
            {
                Application.quitting -= OnQuit;
                _isQuiting = false;
                if (change == PlayModeStateChange.ExitingPlayMode)
                {
                    _isQuiting = true;
                    ClearAllEditor();
                }
            }

            EditorApplication.playModeStateChanged -= OnChange;
            EditorApplication.playModeStateChanged += OnChange;
        }
#endif

        [RuntimeInitializeOnLoadMethod]
        static void RunOnStart()
        {
            _isQuiting = false;
            bool quitSubscribe = true;
#if UNITY_EDITOR
            quitSubscribe = Application.isPlaying;
#endif
            if (quitSubscribe)
            {
                Application.quitting -= OnQuit;
                Application.quitting += OnQuit;
            }
        }

        private static void OnQuit()
        {
            _isQuiting = true;
        }

        public static bool IsEmpty
        {
            get { return _singletonMaps.Count == 0 && _transientMaps.Count == 0; }
        }

        public static bool IsValidService<T>(T objectToCheck)
            where T : class
        {
            if (objectToCheck == null)
            {
                if (!_isQuiting)
                {
                    Debug.LogWarning(typeof(T).Name + " is missing! Make sure configurator can point to it.");
                }

                return false;
            }

            foreach (var instances in _singletonInstances.Values)
            {
                if (objectToCheck == instances)
                {
                    return true;
                }
            }

            var typeOfT = objectToCheck.GetType();
            foreach (var transientTypeValue in _transientMaps.Values)
            {
                if (typeOfT == transientTypeValue)
                {
                    return true;
                }
            }

            return false;
        }

        public static void HandleNewSceneLoaded()
        {
            List<IPersistantSingleton> multis = new List<IPersistantSingleton>();
            foreach (KeyValuePair<Type, object> pair in _singletonInstances)
            {
                IPersistantSingleton multi = pair.Value as IPersistantSingleton;
                if (multi != null)
                    multis.Add(multi);
            }

            foreach (var multi in multis)
            {
                MonoBehaviour behavior = multi as MonoBehaviour;
                if (behavior != null)
                {
                    behavior.StartCoroutine(multi.HandleNewSceneLoaded());
                }
            }
        }

        /// <summary>
        /// Use it at Runtime on scene switching or context switching to update null ref!
        /// </summary>
        public static void ResetNUllServices()
        {
            List<Type> survivorRegisteredTypes = new List<Type>();
            List<System.Object> survivors = new List<System.Object>();
            foreach (KeyValuePair<Type, System.Object> pair in _singletonInstances)
            {
                if (pair.Value != null && pair.Value is IPersistantSingleton)
                {
                    survivors.Add(pair.Value);
                    survivorRegisteredTypes.Add(pair.Key);
                }
            }

            _singletonMaps.Clear();
            _transientMaps.Clear();
            _singletonInstances.Clear();

            for (int i = 0; i < survivors.Count; i++)
            {
                _singletonInstances[survivorRegisteredTypes[i]] = survivors[i];
                _singletonMaps[survivorRegisteredTypes[i]] = survivors[i].GetType();
            }
        }

        /// <summary>
        /// Use it only for Tests
        /// </summary>
        public static void ClearAllEditor()
        {
            _actualNonTransientChangedEvent.Clear();
            _singletonMaps.Clear();
            _transientMaps.Clear();
            _singletonInstances.Clear();
            EventBus.ResetAll();
            UniRx.MessageBroker.Default.Buffered().ClearAllBuffer();
            MessageRouter.Reset();
            _onTotalCleanUp?.Invoke();
            _onTotalCleanUp = NonAllocatedCleanUpLambda;
        }

        public static void RegisterMainConfiguration<TConcrete>()
            where TConcrete : IServiceLocatorConfigure
        {
            RegisterSingleton<TConcrete>();
            Remap<IServiceLocatorConfigure, TConcrete>();
            RegisterTransient<IServiceLocatorConfigure>(typeof(TConcrete));
        }

        public static void RegisterTestConfigurationAsMain<TConcrete>(TConcrete objectToUse)
            where TConcrete : IServiceLocatorConfigure
        {
            RegisterMainConfiguration<TConcrete>();
            Remap<IServiceLocatorConfigureTest, TConcrete>();
            if (objectToUse != null)
            {
                RegisterSingleton<IServiceLocatorConfigure>(objectToUse);
            }
        }

        private static void RegisterSingleton<TConcrete>()
        {
            _singletonMaps[typeof(TConcrete)] = typeof(TConcrete);
        }

        /// <summary>
        /// Maps one type to another
        /// </summary>
        /// <typeparam name="TAbstract"></typeparam>
        /// <typeparam name="TConcrete"></typeparam>
        public static void Remap<TAbstract, TConcrete>()
        {
            Remap(typeof(TAbstract), typeof(TConcrete));
        }

        /// <summary>
        /// Maps one type to another
        /// </summary>
        /// <typeparam name="TAbstract"></typeparam>
        /// <typeparam name="TConcrete"></typeparam>
        public static void Remap<TAbstract>(Type TConcrete)
        {
            Remap(typeof(TAbstract), TConcrete);
        }

        /// <summary>
        /// Maps one type to another
        /// </summary>
        /// <typeparam name="TAbstract"></typeparam>
        /// <typeparam name="TConcrete"></typeparam>
        private static void Remap(Type TAbstract, Type TConcrete)
        {
            _singletonMaps[TAbstract] = TConcrete;
        }

        /// <summary>
        /// Same As Remap with Registered service. Always rewrites the current service
        /// </summary>
        public static void RegisterSingleton<TAbstract>(TAbstract instance, bool skipNullCheck = false)
        {
            if (instance == null && !skipNullCheck)
            {
                return;
            }

            var targetType = typeof(TAbstract);
            _singletonMaps[targetType] = targetType;
            _singletonInstances[targetType] = instance;
            InvokeNonTransientServiceChange(targetType, instance);
        }

        public static void UnRegisterSingletonInstance<TAbstract>(TAbstract instance)
        {
            var targetType = typeof(TAbstract);

            if (_singletonInstances.TryGetValue(targetType, out var singleton)
                && singleton == (System.Object) instance)
            {
                _singletonInstances[targetType] = null;
            }
        }

        /// <summary>
        /// Each Transient is a unique object
        /// </summary>
        /// <typeparam name="TAbstract"></typeparam>
        /// <typeparam name="TConcrete"></typeparam>
        public static void RegisterTransient<TAbstract, TConcrete>()
        {
            RegisterTransient(typeof(TAbstract), typeof(TConcrete));
        }

        /// <summary>
        /// Each Transient is a unique object
        /// </summary>
        /// <typeparam name="TAbstract"></typeparam>
        /// <typeparam name="TConcrete"></typeparam>
        public static void RegisterTransient<TAbstract>(Type TConcrete)
        {
            RegisterTransient(typeof(TAbstract), TConcrete);
        }

        /// <summary>
        /// Each Transient is a unique object
        /// </summary>
        /// <typeparam name="TAbstract"></typeparam>
        /// <typeparam name="TConcrete"></typeparam>
        public static void RegisterTransient(Type TAbstract, Type TConcrete)
        {
            if (!_transientMaps.ContainsKey(TAbstract))
            {
                _transientMaps.Add(TAbstract, null);
            }

            _transientMaps[TAbstract] = TConcrete;
        }

        public static T Resolve<T>() where T : class
        {
            return ResolveWith<T>(true);
        }

        public static T ResolveOrCreate<T>() where T : class
        {
            return ResolveWith<T>(false);
        }

        private static T ResolveWith<T>(bool onlyExisting) where T : class
        {
            if (_isQuiting)
            {
                return null;
            }

            var currentlyLookingType = typeof(T);

            if (!_singletonInstances.ContainsKey(_typeOfMainLocatorInterface)
                || _singletonInstances[_typeOfMainLocatorInterface] == null)
            {
                if (InternalResolve<IServiceLocatorConfigure>(onlyExisting, _typeOfMainLocatorInterface) == null)
                {
                    var newConfiguration = ServiceFactoryConfigure();
                    if (newConfiguration == null)
                    {
                        newConfiguration = typeof(SampleServiceLocatorConfiguration);
                    }

                    IServiceLocatorConfigure concreteConfig = null;
                    if (newConfiguration.IsSubclassOf(typeof(MonoBehaviour)))
                    {
                        GameObject singletonGameObject = new GameObject();
                        concreteConfig = (IServiceLocatorConfigure) singletonGameObject.AddComponent(newConfiguration);
                        singletonGameObject.name = _typeOfMainLocatorInterface.ToString() + " (singleton)";
                    }
                    else
                    {
                        concreteConfig = (IServiceLocatorConfigure) Activator.CreateInstance(newConfiguration);
                    }

                    RegisterSingleton(concreteConfig);
                    Remap<IServiceLocatorConfigure>(newConfiguration);
                }
            }

            T result = InternalResolve<T>(onlyExisting, currentlyLookingType);
            return result;
        }

        private static T InternalResolve<T>(bool onlyExisting, System.Type typeOfGeneric) where T : class
        {
            T result = default(T);
            Type concreteType = null;
            if (_singletonMaps.TryGetValue(typeOfGeneric, out concreteType))
            {
                System.Object r = null;
                if (!_singletonInstances.TryGetValue(typeOfGeneric, out r) && !onlyExisting)
                {
                    if (concreteType.IsSubclassOf(typeof(MonoBehaviour)))
                    {
                        GameObject singletonGameObject = new GameObject();
                        r = singletonGameObject.AddComponent(concreteType);
                        singletonGameObject.name = typeOfGeneric.ToString() + " (singleton)";
                    }
                    else
                    {
                        r = Activator.CreateInstance(concreteType);
                    }

                    _singletonInstances[typeOfGeneric] = r;
                    IPersistantSingleton multi = r as IPersistantSingleton;
                    if (multi != null)
                    {
                        multi.HandleNewSceneLoaded();
                    }
                }

                result = (T) r;
            }
            else if (_transientMaps.TryGetValue(typeOfGeneric, out concreteType))
            {
                System.Object r = Activator.CreateInstance(concreteType);
                result = (T) r;
            }

            return result;
        }

        private static bool IsRuntimePlaying()
        {
            bool result = true;
#if UNITY_EDITOR
            result = Application.isPlaying;
#endif
            return result;
        }

        private static Type ServiceFactoryConfigure()
        {
            var resourcesLoad = Resources.LoadAll("Managers");
            UnityEngine.Object[] components = null;
            Type result = null;
#if UNITY_EDITOR
            if (!IsRuntimePlaying())
            {
                // Sort
                LinkedList<UnityEngine.Object> sorted = new LinkedList<UnityEngine.Object>();
                foreach (var item in resourcesLoad)
                {
                    bool added = false;
                    if (item is ScriptableObject)
                    {
                        if (item is IServiceLocatorConfigureTest)
                        {
                            sorted.AddFirst(item);
                            added = true;
                        }
                    }
                    else
                    {
                        components = (item as GameObject).GetComponentsInChildren<Component>();
                        foreach (var component in components)
                        {
                            if (component is IServiceLocatorConfigureTest)
                            {
                                sorted.AddFirst(item);
                                added = true;
                                break;
                            }
                        }
                    }

                    if (!added)
                    {
                        sorted.AddLast(item);
                    }
                }

                sorted.CopyTo(resourcesLoad, 0);
            }
#endif

            foreach (var item in resourcesLoad)
            {
                if (item is ScriptableObject)
                {
                    components = new[] {item};
                }
                else
                {
                    components = (item as GameObject).GetComponentsInChildren<Component>();
                }

                foreach (var component in components)
                {
                    if (result == null && component is IServiceLocatorConfigure)
                    {
                        var asInterface = (IServiceLocatorConfigure) component;
                        result = asInterface.GetConfigurator();
                    }
                }
            }

            resourcesLoad = null;
            Resources.UnloadUnusedAssets();

            return result;
        }
    }
}