using System.Text;
using Unity.AutomatedQA;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace TestPlatforms.Cloud
{
    [CustomEditor(typeof(CloudTestDeviceInput))]
    public class CloudTestDeviceInputEditor: UnityEditor.Editor
    {
        
        SerializedProperty deviceOSVersion;
        private SerializedProperty deviceNames;
        
        void OnEnable()
        {
            deviceOSVersion = serializedObject.FindProperty("deviceOSVersion");
            deviceNames = serializedObject.FindProperty("deviceNames");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            base.OnInspectorGUI();

            var cloudTestDeviceInput = (CloudTestDeviceInput) target;

            var dsiObj = new DeviceSelectionInformation(cloudTestDeviceInput);

            var dsiPayload = JsonUtility.ToJson(dsiObj);

            if (GUILayout.Button("Validate"))
            {
                var url = $"{AutomatedQASettings.DEVICE_TESTING_API_ENDPOINT}/v1/devices/verify?projectId={Application.cloudProjectId}";
                UploadHandlerRaw uH = new UploadHandlerRaw(Encoding.UTF8.GetBytes(dsiPayload));
                uH.contentType = "application/json";
                
                using (var uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    uwr.uploadHandler = uH;
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    uwr.SetRequestHeader("Content-Type", "application/json");
                    uwr.SetRequestHeader("Authorization", "Bearer " + CloudProjectSettings.accessToken);

                    AsyncOperation request = uwr.SendWebRequest();

                    while (!request.isDone)
                    {
                        EditorUtility.DisplayProgressBar("Run Cloud Tests", "Starting tests", request.progress);
                    }
                    EditorUtility.ClearProgressBar();

                    if (uwr.IsError())
                    {
                        Debug.Log($"Couldn't start cloud tests. Error code {uwr.error} with message - {uwr.downloadHandler.text}");
                    }
                    else
                    {
                        string response = uwr.downloadHandler.text;
                        Debug.Log($"response: {response}");
                        // return JsonUtility.FromJson<JobStatusResponse>(response);
                    }
                }
            }

        }
    }
}