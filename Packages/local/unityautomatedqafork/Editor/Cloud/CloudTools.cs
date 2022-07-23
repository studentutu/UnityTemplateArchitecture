using System.Collections.Generic;
using System.IO;
using Unity.CloudTesting.Editor;
using UnityEngine;


namespace Unity.CloudTesting.Editor
{
    public class CloudTools
    {
        public static void UploadAllRecordings(List<RecordingUploadWindow.TestAndRecording> testAndRecordings)
        {
            foreach (RecordingUploadWindow.TestAndRecording tr in testAndRecordings)
            {
                UploadRecording(tr, true);
            }
        }
        
        internal static void UploadRecording(RecordingUploadWindow.TestAndRecording tr, bool silent = false)
        {
            var recordingFile = tr.GetRecordingPath();
            if (!File.Exists(recordingFile))
            {
                Debug.Log($"File {recordingFile} does not exist");
                return;
            }
            var testName = tr.GetTest();
            EditorClient.UploadRecording(testName, recordingFile);
            Debug.Log($"Upload {testName} complete");

            if (!silent)
            {
                Debug.Log("Successfully uploaded recording for " + testName);
            }
        }
    }
}