using System;
using System.Collections;
using System.Text;
using Unity.AutomatedQA;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.RecordedTesting.Runtime
{

    public static class RuntimeClient
    {
        
        public static void LogTestCompletion(string testName)
        {
            var logger = new AQALogger();
            logger.Log("Unity Test Completed: " + testName);
        }


        public static void DownloadRecording(string recordingFileName, string resultFileOutputPath)
        {
            var logger = new AQALogger();

            var projectId = Application.cloudProjectId;
            var downloadUri =
                $"{AutomatedQASettings.DEVICE_TESTING_API_ENDPOINT}/v1/recordings/{recordingFileName}/download?projectId={projectId}";

            var dh = new DownloadHandlerFile(resultFileOutputPath);

            dh.removeFileOnAbort = true;
            logger.Log("Starting download" + downloadUri);
            using (var webrx = UnityWebRequest.Get(downloadUri))
            {

                webrx.downloadHandler = dh;
                AsyncOperation request = webrx.SendWebRequest();

                while (!request.isDone)
                {
                }

                if (webrx.IsError())
                {
                    logger.LogError($"Couldn't download file. Error - {webrx.error}");
                }
                else
                {
                    logger.Log($"Downloaded file saved to {resultFileOutputPath}.");
                }

            }
        }
    }
}