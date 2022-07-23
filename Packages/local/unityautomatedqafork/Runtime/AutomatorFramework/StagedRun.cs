using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.AutomatedQA
{
    [CreateAssetMenu(fileName = "StagedRun", menuName = "Automated QA/Staged Run", order = 3)]
    public class StagedRun: ScriptableObject
    {
        [Serializable]
        public class Stage
        {
            [SerializeReference]
            public List<AutomatorConfig> automators = new List<AutomatorConfig>();
        }

        public List<Stage> stages = new List<Stage>();

        /// <summary>
        /// Wrapper for stage sequence calculation
        /// </summary>
        public List<List<AutomatorConfig>> CalculateStageSequences()
        {
            var result = new List<List<AutomatorConfig>>();
            CalculateStageSequences(stages, result, new List<AutomatorConfig>());
            return result;
        }
        
        /// <summary>
        /// Calculate sequences of Automators given stages
        /// </summary>
        /// <param name="stages">Stages of Automators</param>
        /// <param name="resultSequences"></param>
        /// <param name="currentSequence"></param>
        private void CalculateStageSequences(List<Stage> stages, List<List<AutomatorConfig>> resultSequences, List<AutomatorConfig> currentSequence)
        {
            // Algorithm has finished last stage
            if (stages.Count == 0)
            {
                // Include if accumulation has started
                if (currentSequence.Count > 0)
                {
                    resultSequences.Add(currentSequence);
                }
            }
            else
            {
                // Recurse on the rest of the stages
                foreach (var automator in stages[0].automators)
                {
                    var currSequenceCopy = new List<AutomatorConfig>(currentSequence);
                    currSequenceCopy.Add(automator);
                    
                    var remainingStages = stages.Skip(1).ToList();
                    CalculateStageSequences(remainingStages, resultSequences, currSequenceCopy);
                }
            }
        }
    }
}