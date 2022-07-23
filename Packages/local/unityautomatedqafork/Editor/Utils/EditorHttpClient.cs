using Unity.AutomatedQA;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Utils
{
    /// <summary>
    /// An extended http client that displays a progress bar while processing a request in editor
    /// </summary>
    /// <inheritdoc cref="HttpClient"/>
    public class EditorHttpClient: HttpClientImpl
    {
        
        protected override string SendRequestBlocking(UnityWebRequest uwr)
        {
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            
            AsyncOperation response = uwr.SendWebRequest();
            
            while (!response.isDone)
            {
                EditorUtility.DisplayProgressBar("Processing Request", $"Processing {uwr.method} request", response.progress);
            }
            EditorUtility.ClearProgressBar();
            
            if (uwr.IsError())
            {
                AutomatedQaTools.HandleError($"Could not process http request Error: {uwr.error}: {uwr.downloadHandler.text} for url: {uwr.url}");
            }
            
            return uwr.downloadHandler.text;
        }
        
    }
}