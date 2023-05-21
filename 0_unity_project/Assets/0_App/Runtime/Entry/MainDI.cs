using App.Runtime.MVC.Feature1;
using UnityEngine;
using Zenject;

namespace _0_App.Runtime.Entry
{
    /// <summary>
    ///     Main object to start all installations.
    /// </summary>
    [ExecuteAfter(typeof(Zenject.ZenjectStateMachineBehaviourAutoInjecter))]
    public class MainDI: MonoBehaviour
    {
        public static DiContainer Get()
        {
            return ProjectContext.Instance.Container;
        }

        public void Awake()
        {
            var instance = ProjectContext.Instance;
            var container = instance.Container;

            ReactiveClickerInstaller.Install(container);
        }
    }
}