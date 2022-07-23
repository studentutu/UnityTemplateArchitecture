using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unity.AutomatedQA
{
    [Serializable]
    public class AutomatorConfig
    {
        public virtual Type AutomatorType => null;
        internal AutomatorConfig() { }
    }

    [Serializable]
    public abstract class AutomatorConfig<T> : AutomatorConfig where T : Automator
    {
        public sealed override Type AutomatorType => typeof(T);
    }
  
}
