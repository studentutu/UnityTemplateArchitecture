using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.AutomatedQA;
using Unity.RecordedPlayback.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace Unity.CloudTesting.Editor
{
    public static class EditorClient
    {
        public static List<Recording> ListRecordings()
        {
            var projectId = Application.cloudProjectId;
            var url = $"{AutomatedQASettings.DEVICE_TESTING_API_ENDPOINT}/v1/recordings/list?projectId={projectId}";
            using (var webrx = UnityWebRequest.Get(url))
            {
                webrx.SetRequestHeader("Authorization", "Bearer " + CloudProjectSettings.accessToken);

                webrx.SendWebRequest();
                while (!webrx.isDone)
                {
                }

                if (webrx.IsError())
                {
                    throw new Exception("Failed to generate upload URL with error: " + webrx.error + "\n" + webrx.downloadHandler.text);
                }

                var data = JsonUtility.FromJson<RecordingsList>(webrx.downloadHandler.text);
                return data.recordings;
            }
        }
        
        private static string FormatRecording(string recordingName, string filePath)
        {
            var currRecording = RecordingInputModule.InputModuleRecordingData.FromFile(filePath);
            if (currRecording.recordings.Count > 0)
            {
                // create temporary file that copies original
                Debug.Log($"extractRecordingsFromComposite: {recordingName} {filePath}");
                var tempFilePath = $"{AutomatedQASettings.RecordingFolderNameWithAssetPath}/Temp/{recordingName}";
                var segmentDir = Path.GetDirectoryName(filePath) ?? "";
                currRecording.touchData = currRecording.GetAllTouchData(segmentDir: segmentDir);
                currRecording.recordings.Clear();
                RecordedPlaybackEditorUtils.CreateDirectoryIfNotExists(Path.GetDirectoryName(tempFilePath));
                currRecording.SaveToFile(tempFilePath);
                RecordedPlaybackEditorUtils.MarkFileAsCreated(tempFilePath);

                return tempFilePath;
            }

            return filePath;
        }

        public static void UploadRecording(string recordingName, string filePath)
        {
            var projectId = Application.cloudProjectId;
            var url = $"{AutomatedQASettings.DEVICE_TESTING_API_ENDPOINT}/v1/recordings?projectId={projectId}";

            Debug.Log($"Uploading file {filePath} as {recordingName}");
            
            // if composite recording, combine touchData for all referenced recordings
            try
            {
                var targetFilePath = FormatRecording(recordingName, filePath);
                Transaction.Upload(url, recordingName, targetFilePath);
            }
            finally
            {
                RecordedPlaybackEditorUtils.ClearCreatedPaths();
            }
        }

        internal static class Transaction
        {
            public static void Upload(string url, string recordingName, string filePath)
            {
                Upload(url, recordingName, File.ReadAllBytes(filePath));
            }

            public static void Upload(string url, string name, byte[] data, bool useTransferUrls = true)
            {
                Action<UnityWebRequest> action = (UnityWebRequest webrx) =>
                {
                    webrx.uploadHandler = new UploadHandlerRaw(data);
                    webrx.SendWebRequest();
                    while (!webrx.isDone)
                    {
                    }

                    if (webrx.IsError())
                    {
                        Debug.LogError("Failed to upload with error \n" + webrx.error + "\n" + webrx.downloadHandler.text);
                        return;

                    }

                    if (!string.IsNullOrEmpty(webrx.downloadHandler.text))
                    {
                        Debug.Assert(false, "Need to pull id from response");
                        // set entity return id here
                    }
                };

                if (useTransferUrls)
                {
                    var uploadUrl = GetUploadURL(url, name);
                    using (var webrx = UnityWebRequest.Put(uploadUrl, data))
                    {
                        action(webrx);
                    }
                }
                else
                {
                    using (var webrx = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                    {
                        action(webrx);
                    }
                }
            }

            public static string GetUploadURL(string url, string path)
            {
                var payload = JsonUtility.ToJson(new UploadInfo(Path.GetFileName(path)));

                using (var webrx = UnityWebRequest.Post(url, payload))
                {
                    webrx.SetRequestHeader("Content-Type", "application/json");
                    webrx.SetRequestHeader("Authorization", "Bearer " + CloudProjectSettings.accessToken);

                    webrx.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                    webrx.timeout = 30;
                    webrx.SendWebRequest();
                    while (!webrx.isDone)
                    {
                    }

                    if (webrx.IsError())
                    {
                        throw new Exception("Failed to generate upload URL with error: " + webrx.error + "\n" + webrx.downloadHandler.text);
                    }

                    var data = JsonUtility.FromJson<UploadUrlData>(webrx.downloadHandler.text);
                    return data.upload_uri;
                }
            }
        }
    }
    
    [Serializable]
    public struct Recording
    {
        public string name;
        public string md5;
    }
    
    [Serializable]
    internal struct UploadInfo
    {
        public string name;
        public UploadInfo(string name)
        {
            this.name = name;
        }
    }

#pragma warning disable CS0649
    [Serializable]
    internal struct UploadUrlData
    {
        public string upload_uri;
    }

    [Serializable]
    internal struct RecordingsList
    {
        public List<Recording> recordings;
    }
#pragma warning restore CS0649
}