using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Core
{
    [Serializable]
    [CreateAssetMenu(fileName = "EditorPlayerPrefs", menuName = "Tools/EditorPlayerPrefs", order = 3)]
    public class SerializedFullPlayerPrefsFromEditor : ScriptableObject
    {
        public List<PlayerPrefsSerializedEntry> PlayerPrefsEntries = new List<PlayerPrefsSerializedEntry>();

        public enum PlayerPrefsEnumValueType
        {
            INTEGER,
            STRING
        }

        [Serializable]
        public class PlayerPrefsSerializedEntry
        {
            public string key = null;
            public string value = null;
            public PlayerPrefsEnumValueType actualType = PlayerPrefsEnumValueType.STRING;
        }
    }

    public class PlayerPrefsExtensions
    {
    }
}