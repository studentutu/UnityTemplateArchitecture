using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.RecordedPlayback;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.AutomatedQA
{
    public class CentralAutomationController : MonoBehaviour
    {
        private AutomatedRun.RunConfig runConfig = null;

        private List<Automator> automators = new List<Automator>();
        private int currentIndex = 0;
        public static bool Initialized { get; set; }

        private static CentralAutomationController _instance = null;
        public static CentralAutomationController Instance {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(typeof(CentralAutomationController).ToString());
                    _instance = go.AddComponent<CentralAutomationController>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        public void Run(AutomatedRun.RunConfig runConfig = null)
        {
            if (Initialized)
            {
                return;
            }

            if (runConfig == null)
            {
                runConfig = new AutomatedRun.RunConfig();
            }

            Initialized = true;
            this.runConfig = runConfig;

            foreach (var automatorConfig in runConfig.automators)
            {
                AddAutomator(automatorConfig);
            }
            BeginAutomator();

        }

        public static bool Exists()
        {
            return _instance != null;
        }
        
        public T AddAutomator<T>() where T : Automator
        {
            var go = new GameObject(typeof(T).ToString());
            go.transform.SetParent(transform);
            var automator = go.AddComponent<T>();
            automators.Add(automator);
            SubscribeEvents(automator);
            automator.Init();
            return automator;
        }
        
        public T AddAutomator<T>(AutomatorConfig config) where T : Automator
        {
            var go = new GameObject(typeof(T).ToString());
            go.transform.SetParent(transform);
            var automator = go.AddComponent<T>();
            automators.Add(automator);
            SubscribeEvents(automator);
            automator.Init(config);
            return automator;
        }
        
        public Automator AddAutomator(Type AutomatorType)
        {
            var go = new GameObject(AutomatorType.ToString());
            go.transform.SetParent(transform);
            var automator = go.AddComponent(AutomatorType) as Automator;
            automators.Add(automator);
            SubscribeEvents(automator);
            automator.Init();
            return automator;
        }

        public T AddAutomator<T>(T prefab) where T : Automator
        {
            var automator = Instantiate(prefab, transform);
            automators.Add(automator);
            SubscribeEvents(automator);
            automator.Init();
            return automator;
        }

        public Automator AddAutomator(AutomatorConfig config)
        {
            var automator = AddAutomator(config.AutomatorType);
            automator.Init(config);
            return automator;
        }

        public void ResetAutomators()
        {
            currentIndex = 0;
            Initialized = false;
            automators = new List<Automator>();
        }

        public void Reset()
        {
            foreach (var automator in automators)
            {
                automator.Cleanup();
                Destroy(automator.gameObject);
            }

            runConfig = null;
            automators = new List<Automator>();
            currentIndex = 0;
            Initialized = false;

            Destroy(gameObject);
            _instance = null;
        }

        private void SubscribeEvents(Automator automator)
        {
            automator.OnAutomationFinished.AddListener(OnAutomationFinished);
        }

        private void OnAutomationFinished(Automator.AutomationFinishedEvent.Args args)
        {
            currentIndex++;

            if (currentIndex >= automators.Count)
            {
                // If this is a single test run from an Automated Run editor window, finalize the report.
                if (ReportingManager.IsPlaybackStartedFromEditorWindow)
                    ReportingManager.FinalizeReport();

                if (runConfig.quitOnFinish)
                {
#if UNITY_EDITOR
                    EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
                }
            }
            
            BeginAutomator();
        }
        
        void BeginAutomator()
        {
            if (currentIndex >= 0 && currentIndex < automators.Count)
            {
                automators[currentIndex].BeginAutomation();
            }
        }

        public bool IsAutomationComplete()
        {
            return currentIndex >= automators.Count;
        }

        public T GetAutomator<T>() where T : Automator
        {
            var results = GetAutomators<T>();
            if (results.Count > 0)
            {
                return results[0];
            }

            return null;
        }
        
        public List<T> GetAutomators<T>() where T : Automator
        {
            List<T> results = new List<T>();

            foreach (var automator in automators)
            {
                if (automator is T)
                {
                    results.Add((T) automator);
                }
            }

            return results;
        }

        public List<Automator> GetAllAutomators()
        {
            return automators;
        }
    }
}
