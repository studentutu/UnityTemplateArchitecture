using System;
using System.IO;
using System.Reflection;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity.RecordedPlayback.Editor
{

    [CustomEditor(typeof(UnityEngine.EventSystems.RecordingInputModule))]
    public class RecordingInputModuleEditor : UnityEditor.Editor
    {
        private bool is_paused = false;
        private const string pause_signal = "UNITY_EDITOR_PAUSE";

        private bool needs_sync = false;

        private RecordingMode _configOptions;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            TogglePlayPause();
            ShowRecordingFile();
        }

        void TogglePlayPause()
        {
            string button_text = is_paused ? "Play" : "Pause";
            if (GUILayout.Button(button_text))
            {
                is_paused = !is_paused;

                var module = (UnityEngine.EventSystems.RecordingInputModule) target;

                Debug.Log(button_text);

                if (is_paused)
                {
                    module.Pause(pause_signal);
                    needs_sync = true;
                    Debug.Log("set sync");

                    var field = typeof(RecordingInputModule).GetField("_configOptions",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    field.SetValue(target, RecordingMode.None);
                }
                else
                {
                    module.SendSignal(pause_signal);

                    if (needs_sync)
                    {
                        Debug.Log("needed sync");
                        needs_sync = false;

                        var field = typeof(RecordingInputModule).GetField("recordedPlaybackConfigFilename",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        var recordedPlaybackConfigFilename = (string) field.GetValue(module);
                        var configJson = File.ReadAllText(Path.Combine(AutomatedQASettings.PersistentDataPath,
                            recordedPlaybackConfigFilename));
                        _configOptions = JsonUtility.FromJson<RecordingConfig>(configJson).mode;

                        Debug.Log(configJson);

                        var configField = typeof(RecordingInputModule).GetField("_configOptions",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        configField.SetValue(target, _configOptions);
                    }

                    Debug.Log("else");
                }

                var test = typeof(RecordingInputModule).GetField("_configOptions",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Debug.Log(test.GetValue(target));
            }
        }

        void ShowRecordingFile()
        {
            if (GUILayout.Button("Show Recording File"))
            {
                var module = (UnityEngine.EventSystems.RecordingInputModule) target;
                EditorUtility.RevealInFinder(RecordedPlaybackPersistentData.GetRecordingDataFilePath());
            }
        }


        public void OnEnable()
        {
        }
        

    }
}