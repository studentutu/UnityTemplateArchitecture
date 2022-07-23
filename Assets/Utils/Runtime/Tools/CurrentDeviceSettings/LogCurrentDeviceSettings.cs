using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Core
{
    public class LogCurrentDeviceSettings : MonoBehaviour
    {
        public static string QualityString
        {
            get { return QualitySettings.names[QualitySettings.GetQualityLevel()]; }
        }

        private async void OnEnable()
        {
            var top = QualitySettings.names.Length;
            top -= 1;
            var currentLevel = QualitySettings.GetQualityLevel();
            Debug.LogWarning("Current Quality Level : " + QualitySettings.GetQualityLevel() + " From " + top);
            await UniTask.DelayFrame(60);
            await UniTask.SwitchToMainThread();
            if (currentLevel != top)
            {
                QualitySettings.SetQualityLevel(top, true);
            }
        }
    }
}