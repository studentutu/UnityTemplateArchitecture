using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.AutomatedQA
{
    [CreateAssetMenu(fileName = "AutomatedRun", menuName = "Automated QA/Automated Run", order = 1)]
    public class AutomatedRun : ScriptableObject
    {
        [Serializable]
        public class RunConfig
        {
            public bool quitOnFinish = false;

            [SerializeReference]
            public List<AutomatorConfig> automators = new List<AutomatorConfig>();
        }

        public RunConfig config = new RunConfig();
    }

}
