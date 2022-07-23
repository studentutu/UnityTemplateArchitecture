using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Unity.AutomatedQA.Editor;
using Unity.RecordedTesting;

namespace Unity.CloudTesting.Editor
{
    public class RecordingUploadWindow: EditorWindow
    {
        private List<TestAndRecording> testAndRecordings = new List<TestAndRecording>();
        private string errorText;
        private string infoText;
        private GUIStyle defaultStyle;
        private GUIStyle redStyle;

    //    [MenuItem("Automated QA/Recording Upload...", priority=AutomatedQABuildConfig.MenuItems.RecordingUpload)]
        public static void ShowWindow()
        {
            RecordingUploadWindow wnd = GetWindow<RecordingUploadWindow>();
            wnd.titleContent = new GUIContent("Cloud Recording Upload");
            SetAllTestsAndRecordings();
        }

        public void OnEnable()
        {
            testAndRecordings = SetAllTestsAndRecordings();
        }

        public void OnGUI()
        {
            defaultStyle = new GUIStyle(EditorStyles.label);
            redStyle = new GUIStyle(EditorStyles.label);
            redStyle.normal.textColor = Color.red;
            
            RenderTestAndRecordings(testAndRecordings);
        }

        internal static List<TestAndRecording> SetAllTestsAndRecordings() 
        {
            Assembly[] assems = AppDomain.CurrentDomain.GetAssemblies();
            List<TestAndRecording> result = new List<TestAndRecording>();
            foreach (Assembly a in assems)
            {
                foreach (Type t in a.GetTypes())
                {
                    foreach (MethodInfo m in t.GetMethods())
                    {
                        foreach (var attribute in m.GetCustomAttributes())
                        {
                            if (attribute.GetType().Equals(typeof(RecordedTestAttribute)))
                            {
                                RecordedTestAttribute r = (RecordedTestAttribute)attribute;
                                var testFullName = m.ReflectedType + "." + m.Name;
                                result.Add(new TestAndRecording(testFullName, r.GetRecording()));
                            }
                        }
                    }

                }
            }
            return result;
        }

        private void RenderTestAndRecordings(List<TestAndRecording> testAndRecordings)
        {
            GUILayout.BeginVertical();
            foreach (TestAndRecording tr in testAndRecordings)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(tr.GetTest());
                EditorGUILayout.LabelField(tr.GetRecording(), tr.FileExists() ? defaultStyle : redStyle);

                GUI.enabled = true; // TODO: compare md5
                if (GUILayout.Button("Upload"))
                {
                    ClearHelpBoxes();
                    CloudTools.UploadRecording(tr);
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Upload All"))
            {
                ClearHelpBoxes();
                foreach (TestAndRecording tr in testAndRecordings)
                {
                    CloudTools.UploadRecording(tr, true);
                }

                infoText = "Upload recordings complete";
            }
            
            if (!string.IsNullOrEmpty(infoText))
            {
                EditorGUILayout.HelpBox(infoText, MessageType.Info);
            }

            if (!string.IsNullOrEmpty(errorText))
            {
                EditorGUILayout.HelpBox(errorText, MessageType.Error);
            }
            
            GUILayout.EndVertical();
        }

        public class TestAndRecording
        {
            private string testName;
            private string recordingName;
            private string md5;
            private bool fileExists;
            private DateTime lastExistsCheck;

            public TestAndRecording(string testName, string recordingName)
            {
                this.testName = testName;
                this.recordingName = recordingName;
                if (FileExists())
                {
                    using (var md5 = MD5.Create())
                    {
                        var recordingFile = GetRecordingPath();
                        using (var stream = File.OpenRead(recordingFile))
                        {
                            var hash = md5.ComputeHash(stream);
                            this.md5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        }
                    }
                }
            }

            public string GetTest()
            {
                return testName;
            }

            public string GetRecording()
            {
                return recordingName;
            }

            public string GetRecordingPath()
            {
                return Path.Combine(Application.dataPath, recordingName);
            }

            public bool FileExists()
            {
                if (lastExistsCheck == null || (DateTime.Now - lastExistsCheck).TotalSeconds > 5)
                {
                    fileExists = File.Exists(GetRecordingPath());;
                    lastExistsCheck = DateTime.Now;
                }
                return fileExists;
            }
        }

        internal void DisplayError(string text)
        {
            Debug.LogError(text);
            errorText = text;
        }

        private void ClearHelpBoxes()
        {
            infoText = "";
            errorText = "";
        }
    }
}