using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace App.Core.Editor
{
    [CreateAssetMenu(menuName = "Core/AndroidSettings", fileName = "AndroidSettings", order = 0)]
    [Serializable]
    public class AndroidSettings : ScriptableObject
    {
        [SerializeField] private string KEYSTORE_PASS = "KEYSTORE_PASS";
        [SerializeField] private string KEY_ALIAS_PASS = "KEY_ALIAS_PASS";
        [SerializeField] private string KEY_ALIAS_NAME = "KEY_ALIAS_NAME";
        [SerializeField] private string PATH_TO_KEYSTORE = "Assets/keystore.keystore";
        [SerializeField] private string androidPackageName = "com.company.app";

        [MenuItem("Tools/Set Local KeyStore")]
        public static void LoadAndUseKeyStore()
        {
            var find = ((AssetDatabase) null).FindAssetsOfType<AndroidSettings>();
            var getFirst = find.FirstOrDefault();
            if (getFirst == null)
            {
                return;
            }

            getFirst.HandleAndroidKeystore();
        }

        private void HandleAndroidKeystore()
        {
#if UNITY_2019_1_OR_NEWER
            PlayerSettings.Android.useCustomKeystore = false;
#endif
            PlayerSettings.Android.keystoreName = PATH_TO_KEYSTORE;

            string keystorePass = KEYSTORE_PASS;
            string keystoreAliasPass = KEY_ALIAS_PASS;

#if UNITY_2019_1_OR_NEWER
            PlayerSettings.Android.useCustomKeystore = true;
#endif
            PlayerSettings.Android.keystoreName = PATH_TO_KEYSTORE;
            PlayerSettings.Android.keyaliasName = KEY_ALIAS_NAME;
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasPass = keystoreAliasPass;

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, androidPackageName);
        }
    }
}