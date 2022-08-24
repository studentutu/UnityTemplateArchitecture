using System;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.AutomatedQA
{
    public abstract class Automator : MonoBehaviour
    {
        internal Automator() { }

        public class AutomationFinishedEvent : UnityEvent<AutomationFinishedEvent.Args>
        {
            public class Args
            {
                public Automator automator;

                public Args(Automator automator)
                {
                    this.automator = automator;
                }
            }
        }
        public enum State
        {
            NOT_STARTED,
            IN_PROGRESS,
            COMPLETE
        }

        public State state { get; internal set; } = State.NOT_STARTED;

        public AutomationFinishedEvent OnAutomationFinished = new AutomationFinishedEvent();


        public abstract void Init();
        public abstract void Init(AutomatorConfig config);

        public virtual void BeginAutomation()
        {
            state = State.IN_PROGRESS;
            ReportingManager.IsAutomatorTest = true;
        }

        public virtual void EndAutomation()
        {
            state = State.COMPLETE;

            OnAutomationFinished.Invoke(new AutomationFinishedEvent.Args(this));
        }

        public virtual void Cleanup() { }
    }

    public abstract class Automator<T> : Automator where T : AutomatorConfig
    {
        protected T config;

        protected AQALogger logger;


        private void Awake()
        {
            logger = new AQALogger();
        }

        public sealed override void Init()
        {
            Init(Activator.CreateInstance<T>());
        }

        public sealed override void Init(AutomatorConfig config)
        {
            Init((T)config);
        }

        public virtual void Init(T config)
        {
            this.config = config;
        }

    }

}