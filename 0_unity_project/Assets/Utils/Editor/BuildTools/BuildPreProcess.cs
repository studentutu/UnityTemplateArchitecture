using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace App.Core.Editor
{
    public class BuildPreProcess
    {
        public class BuildPreProcessAction
        {
            /// <summary>
            /// lower callback order comes first
            /// </summary>
            public int CallbackOrder;
            public System.Action<BuildPlayerOptions> Action;
        }
        private static List<Func<BuildPreProcessAction>> actionBuilderList = new List<Func<BuildPreProcessAction>>();

        public static void AddAction(Func<BuildPreProcessAction> actionToBuild)
        {
            if (actionBuilderList.Contains(actionToBuild))
            {
                return;
            }

            actionBuilderList.Add(actionToBuild);
        }
        
        public static void RemoveActions(Func<BuildPreProcessAction> actionToBuild)
        {
            actionBuilderList.Remove(actionToBuild);
        }
        
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnLastInitializeOnLoad);
        }

        private static void OnLastInitializeOnLoad(BuildPlayerOptions buildPlayerOptions)
        {
            List<BuildPreProcessAction> actionList = new List<BuildPreProcessAction>();
            foreach (var action in actionBuilderList)
            {
                var preProcessAction = action?.Invoke();
                if (preProcessAction != null)
                {
                    actionList.Add(preProcessAction);
                }
            }
            var ordered = actionList.OrderBy(x => x.CallbackOrder);
            foreach (var item in ordered)
            {
                item.Action?.Invoke(buildPlayerOptions);
            }
            // Continue main build
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(buildPlayerOptions);
        }
    }
}