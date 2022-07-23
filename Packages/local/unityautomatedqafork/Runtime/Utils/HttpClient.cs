using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Transactions;
using JetBrains.Annotations;
using Unity.AutomatedQA;
using UnityEngine;
using UnityEngine.Networking;

namespace Utils
{
    /// <summary>
    /// A simple blocking Http Client using UnityWebRequest.
    /// </summary>
    public interface HttpClient
    {
        /// <summary>
        /// A get request that can optionally take an authorization token to be wrapped as Bearer auth.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="authorization"></param>
        /// <returns>Resulting string from download handler. Throws exception only in batch mode.</returns>
        string ProcessHttpGetBlocking(string url, [CanBeNull] string authorization = null);

        /// <summary>
        /// A Post request that can optionally take an authorization token to be wrapped as Bearer auth.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="authorization"></param>
        /// <returns>Resulting string from download handler. Throws exception only in batch mode.</returns>
        string ProcessHttpPostBlocking(string url, UploadHandler uh, [CanBeNull] string authorization = null);

        /// <summary>
        /// A Put request that can optionally take an authorization token to be wrapped as Bearer auth.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="authorization"></param>
        /// <returns>Resulting string from download handler. Throws exception only in batch mode.</returns>
        string ProcessHttpPutBlocking(string url, UploadHandler uh, [CanBeNull] string authorization = null);

    }

    /// <inheritdoc cref="HttpClient"/>
    public class HttpClientImpl: HttpClient
    {

        public string ProcessHttpGetBlocking(string url, [CanBeNull] string authorization = null)
        {

            UnityWebRequest uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);

            if (authorization != null)
            {
                uwr.SetRequestHeader("Authorization", "Bearer " + authorization);
            }

            return SendRequestBlocking(uwr);
        }

        public string ProcessHttpPostBlocking(string url, UploadHandler uh, string authorization = null)
        {
            return ProcessHttpWithDataBlocking(url, uh, UnityWebRequest.kHttpVerbPOST, authorization);
        }

        public string ProcessHttpPutBlocking(string url, UploadHandler uh, string authorization = null)
        {
            return ProcessHttpWithDataBlocking(url, uh, UnityWebRequest.kHttpVerbPUT, authorization);
        }


        private string ProcessHttpWithDataBlocking(string url, UploadHandler uh, string requestType, string authorization = null)
        {
            UnityWebRequest uwr = new UnityWebRequest(url, requestType);
            if (authorization != null)
            {
                uwr.SetRequestHeader("Authorization", "Bearer " + authorization);
            }
            
            uwr.uploadHandler = uh;

            return SendRequestBlocking(uwr);
        }

        protected virtual string SendRequestBlocking(UnityWebRequest uwr)
        {
            
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            
            AsyncOperation response = uwr.SendWebRequest();

            while (!response.isDone)
            {
            }
            
            if (uwr.IsError())
            {
                AutomatedQaTools.HandleError($"Could not process http request Error: {uwr.error}: {uwr.downloadHandler.text} for url: {uwr.url}");
            }
            
            return uwr.downloadHandler.text; 
        }
    }
}