using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.AutomatedQA;
using Unity.RecordedTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Utils;

namespace TestPlatforms.Cloud
{
    /// <summary>
    /// A C# client to execute common device testing API requests
    /// </summary>
    public interface ICloudTestClient
    {
        /// <summary>
        /// Get a signed upload url that can be used to upload a build through a put request
        /// </summary>
        /// <returns>UploadUrlResponse including a buildId and a url</returns>
        UploadUrlResponse GetUploadURL();
        /// <summary>
        /// Get a signed upload url that can be used to upload a build through a put request
        /// </summary>
        /// <returns>UploadUrlResponse including a buildId and a url</returns>
        UploadUrlResponse GetUploadURL(string accessToken, string projectId);
        /// <summary>
        /// Execute a put request to the given uploadURL for an artifact located at buildPath
        /// </summary>
        /// <param name="uploadURL"></param>
        /// <param name="buildPath"></param>
        void UploadBuildToUrl(string uploadURL, string buildPath);
        /// <summary>
        /// Combines GetUploadURL and UploadBuildToURL
        /// </summary>
        /// <param name="buildPath"></param>
        /// <param name="accessToken"></param>
        /// <param name="projectId"></param>
        /// <returns>UploadUrlResponse</returns>
        UploadUrlResponse UploadBuild(string buildPath, string accessToken, string projectId);
        /// <summary>
        /// Runs tests on cloud for a given buildId, test list, and device list.
        /// </summary>
        /// <param name="buildId"></param>
        /// <param name="cloudTests"></param>
        /// <param name="cloudTestSubmission"></param>
        /// <returns>Initial job status</returns>
        JobStatusResponse RunCloudTests(string buildId, List<string> cloudTests, CloudTestDeviceInput cloudTestSubmission);
        /// <summary>
        /// Runs tests on cloud for a given buildId, test list, and device list.
        /// </summary>
        /// <param name="buildId"></param>
        /// <param name="cloudTests"></param>
        /// <param name="cloudTestSubmission"></param>
        /// <returns>Initial job status</returns>
        JobStatusResponse RunCloudTests(string buildId, List<string> cloudTests, CloudTestDeviceInput cloudTestSubmission, string accessToken, string projectId);
        /// <summary>
        /// Gets the status of a job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns>JobStatus</returns>
        JobStatusResponse GetJobStatus(string jobId);
        /// <summary>
        /// Gets the status of a job
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns>JobStatus</returns>
        JobStatusResponse GetJobStatus(string jobId, string accessToken, string projectId);
        /// <summary>
        /// Returns raw cloud test results. This includes a signed location for advanced report data and basic pass fail information
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns>TestResultsResponse</returns>
        TestResultsResponse GetTestResults(string jobId);
        /// <summary>
        /// Returns raw cloud test results. This includes a signed location for advanced report data and basic pass fail information
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns>TestResultsResponse</returns>
        TestResultsResponse GetTestResults(string jobId, string accessToken, string projectId);
        /// <summary>
        /// Get the logs for a given job. These logs will be returned as a signed url for each device of a job. 
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns>GetLogResponse</returns>
        GetLogResponse GetLogs(string jobId);
        /// <inheritdoc cref="GetLogs(string)"/>
        GetLogResponse GetLogs(string jobId, string accessToken, string projectId);
        /// <summary>
        /// Get the existing builds for a project.
        /// </summary>
        /// <returns>The 15 most recent builds from most to least recent</returns>
        GetBuildsResponse GetBuilds();
        /// <summary>
        /// Get the existing builds for a project.
        /// </summary>
        /// <returns>The 15 most recent builds from most to least recent</returns>
        GetBuildsResponse GetBuilds(string accessToken, string projectId);
        /// <summary>
        /// Get the existing jobs for a project
        /// </summary>
        /// <returns>The 15 most recent jobs from most to least recent</returns>
        GetJobsResponse GetJobs();
        GetJobsResponse GetJobs(string accessToken, string projectId);
    }

    /// <inheritdoc cref="ICloudTestClient"/>
    public class CloudTestClient : ICloudTestClient
    {
        private HttpClient client;

        public CloudTestClient(HttpClient client)
        {
            this.client = client;
        }

        public CloudTestClient()
        {
            this.client = new EditorHttpClient();
        }

        public UploadUrlResponse GetUploadURL()
        {
            return GetUploadURL(CloudProjectSettings.accessToken, Application.cloudProjectId);
        }

        public UploadUrlResponse GetUploadURL(string accessToken, string projectId)
        {
            Debug.Log("GetUploadURL");
            var url = $"{AutomatedQASettings.DEVICE_TESTING_API_ENDPOINT}/v1/builds?projectId={projectId}";

            var jsonObject = new GetUploadURLPayload();
#if UNITY_IOS
            jsonObject.buildType = "IOS";
#else
            jsonObject.buildType = "ANDROID";
#endif
            jsonObject.name = CloudTestConfig.BuildName;
            jsonObject.description = "";
            string data = JsonUtility.ToJson(jsonObject);

            byte[] payload = GetBytes(data);
            UploadHandlerRaw uH = new UploadHandlerRaw(payload);
            uH.contentType = "application/json";

            string response = client.ProcessHttpPostBlocking(url, uH, accessToken);
            
            Debug.Log($"response: {response}");
            return JsonUtility.FromJson<UploadUrlResponse>(response);
        }

        public UploadUrlResponse UploadBuild(string buildPath, string accessToken, string projectId)
        {
            var uploadInfo = GetUploadURL(accessToken, projectId);
            UploadBuildToUrl(uploadInfo.upload_uri, buildPath);

            return uploadInfo;
        }

        public void UploadBuildToUrl(string uploadURL, string buildPath)
        {
            Debug.Log($"Upload Build - uploadURL: {uploadURL}");
            Debug.Log($"buildpath: {buildPath}");
            var payload = File.ReadAllBytes(buildPath);
            UploadHandlerRaw uH = new UploadHandlerRaw(payload);
            string response = client.ProcessHttpPutBlocking(uploadURL, uH);
            
            Debug.Log($"Build upload executed with response: ${response}");
        }

        public JobStatusResponse RunCloudTests(string buildId, List<string> cloudTests, CloudTestDeviceInput cloudTestDeviceInput)
        {
            return RunCloudTests(buildId, cloudTests, cloudTestDeviceInput, CloudProjectSettings.accessToken, Application.cloudProjectId);
        }

        public JobStatusResponse RunCloudTests(string buildId, List<string> cloudTests,
            CloudTestDeviceInput cloudTestSubmission, string accessToken, string projectId)
        {
            Debug.Log($"RunCloudTests - buildId: {buildId}");
            var url = $"{AutomatedQASettings.DEVICE_TESTING_API_ENDPOINT}/v1/job/create?projectId={projectId}";

            var jsonObject = new CloudTestPayload();
            jsonObject.buildId = buildId;
            jsonObject.testNames = cloudTests;

            string data = null;
            
            if (cloudTestSubmission is null)
            {
                data = JsonUtility.ToJson(jsonObject);
            }
            else
            {
                var dsi = new DeviceSelectionInformation(cloudTestSubmission);

                var dsiString = JsonUtility.ToJson(dsi);
                var jsonObjectWithDeviceSelection = new CloudTestPayloadWithDeviceSelection(jsonObject, dsiString);
                data = JsonUtility.ToJson(jsonObjectWithDeviceSelection);
            }

            byte[] payload = GetBytes(data);
            UploadHandlerRaw uH = new UploadHandlerRaw(payload);
            uH.contentType = "application/json";

            Debug.Log(url);
            Debug.Log(data);
            
            string response = client.ProcessHttpPostBlocking(url, uH, accessToken);
            
            Debug.Log($"response: {response}");
            return JsonUtility.FromJson<JobStatusResponse>(response);
        }

        public JobStatusResponse GetJobStatus(string jobId)
        {
            return GetJobStatus(jobId, CloudProjectSettings.accessToken, Application.cloudProjectId);
        }

        public JobStatusResponse GetJobStatus(string jobId, string accessToken, string projectId)
        {
            var url = $"{AutomatedQASettings.DEVICE_TESTING_API_ENDPOINT}/v1/jobs/{jobId}?projectId={projectId}";

            string response = client.ProcessHttpGetBlocking(url, accessToken);
            Debug.Log($"response: {response}");

            return JsonUtility.FromJson<JobStatusResponse>(response);
        }

        private string GetSignedUrlContents(string signedUrl)
        {
            string signedUrlContents = client.ProcessHttpGetBlocking(signedUrl);
            
            Debug.Log($"response: {signedUrlContents}");
            
            return signedUrlContents;
        }
        
        public TestResultsResponse GetTestResults(string jobId)
        {
            return GetTestResults(jobId, CloudProjectSettings.accessToken, Application.cloudProjectId);
        }

        public TestResultsResponse GetTestResults(string jobId, string accessToken, string projectId)
        {
            var url = $"{AutomatedQASettings.DEVICE_TESTING_API_ENDPOINT}/v1/counters?jobId={jobId}&projectId={projectId}";
            string response = client.ProcessHttpGetBlocking(url, accessToken);
            Debug.Log($"response: {response}");
            var testResults = JsonUtility.FromJson<TestResultsResponse>(response);
            string outputPath = Path.Combine(AutomatedQASettings.PersistentDataPath, "CloudTestResults", $"TestResults-{jobId}.html");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            TestResultsToXML(testResults);
            // Report for ALL tests, including tests not using the Automated QA test running or reporting logic.
            File.WriteAllText(outputPath, TestResultsToSimpleHTML(jobId, testResults));
            if (!Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(outputPath);
            }
            // Report for only the tests that utilize the Automated QA test runner logic.
            System.Diagnostics.Process.Start(TestResultsToFullHTML(jobId, testResults));

            return testResults;
        }

        public GetLogResponse GetLogs(string jobId)
        {
            return GetLogs(jobId, CloudProjectSettings.accessToken, Application.cloudProjectId);
        }

        public GetLogResponse GetLogs(string jobId, string accessToken, string projectId)
        {
            var url = $"{AutomatedQASettings.DEVICE_TESTING_API_ENDPOINT}/v1/deviceLogs/{jobId}?projectId={projectId}";
            string response = client.ProcessHttpGetBlocking(url, accessToken);
            
            Debug.Log($"response: {response}");
            return JsonUtility.FromJson<GetLogResponse>(response);
        }
        
        public GetBuildsResponse GetBuilds()
        {
            return GetBuilds(CloudProjectSettings.accessToken, Application.cloudProjectId);
        }

        public GetBuildsResponse GetBuilds(string accessToken, string projectId)
        {
            var url = $"{AutomatedQASettings.DEVICE_TESTING_API_ENDPOINT}/v1/builds?projectId={projectId}";
            string response = client.ProcessHttpGetBlocking(url, accessToken);
            
            Debug.Log($"response: {response}");
            return JsonUtility.FromJson<GetBuildsResponse>(response);
        }

        public GetJobsResponse GetJobs()
        {
            return GetJobs(CloudProjectSettings.accessToken, Application.cloudProjectId);
        }
        
        public GetJobsResponse GetJobs(string accessToken, string projectId)
        {
            var url = $"{AutomatedQASettings.DEVICE_TESTING_API_ENDPOINT}/v1/job?projectId={projectId}";
            string response = client.ProcessHttpGetBlocking(url, accessToken);
            
            var res = JsonUtility.FromJson<GetJobsResponse>(response);

            return res;
        }

        private static string TestResultsToSimpleHTML(string jobId, TestResultsResponse data)
        {
            StringBuilder sb = new StringBuilder();
            string overallResult = data.allPass ? "PASS" : "FAIL";
            sb.Append($"<h1>Job ID: {jobId} </h1>");
            sb.Append($"<h2>Overall Result: {overallResult} </h2>");
            sb.Append($"<label>This simple HTML report contains limited results data for all tests in the run. The alternative, full HTML report only includes information on tests that utilized the Automated QA test runner logic.</label>");

            sb.Append($"<table>");
            foreach (var result in data.testResults)
            {
                foreach (var c in result.counters)
                {
                    sb.Append($"<tr>");

                    sb.Append($"<td>{result.deviceModel}</td>");
                    sb.Append($"<td>{result.deviceName}</td>");
                    sb.Append($"<td>{result.testName}</td>");
                    sb.Append($"<td>{c._name}</td>");
                    string passfail = c._value == 1 ? "Pass" : "Fail";
                    string passfailstyle = c._value == 1 ? "background-color: lightgreen" : "background-color: red";
                    sb.Append($"<td style=\"{passfailstyle}\">{passfail}</td>");

                    sb.Append($"</tr>");
                }

            }
            sb.Append($"</table>");
            return sb.ToString();
        }

        private static void HandleError(string msg)
        {
            if (Application.isBatchMode)
            {
                throw new Exception(msg);
            }
            Debug.LogError(msg);
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return bytes;
        }

        private string TestResultsToFullHTML(string jobId, TestResultsResponse data)
        {
            string pathToMultiReport = Path.Combine(ReportingManager.RunSaveDirectory, "CloudReport", jobId).Replace("\\", "/");
            string pathToReports = Path.Combine(pathToMultiReport, "DeviceReports").Replace("\\", "/");
            string pathToReportImgs = Path.Combine(pathToReports, "imgs").Replace("\\", "/");
            if (Directory.Exists(pathToMultiReport))
                Directory.Delete(pathToMultiReport, true);

            Directory.CreateDirectory(pathToMultiReport);
            Directory.CreateDirectory(pathToReports);
            Directory.CreateDirectory(pathToReportImgs);

            File.Copy(Path.GetFullPath("Packages/com.unity.automated-testing/Editor/Cloud/CloudReportImages/android_phone.png"), Path.Combine(pathToReportImgs, "android_phone.png"), true);
            File.Copy(Path.GetFullPath("Packages/com.unity.automated-testing/Editor/Cloud/CloudReportImages/android_tablet.png"), Path.Combine(pathToReportImgs, "android_tablet.png"), true);
            File.Copy(Path.GetFullPath("Packages/com.unity.automated-testing/Editor/Cloud/CloudReportImages/ios_phone.png"), Path.Combine(pathToReportImgs, "ios_phone.png"), true);
            File.Copy(Path.GetFullPath("Packages/com.unity.automated-testing/Editor/Cloud/CloudReportImages/ios_tablet.png"), Path.Combine(pathToReportImgs, "ios_tablet.png"), true);

            StringBuilder sb = new StringBuilder();
            sb.Append(MultiDeviceReportHtmlManifest.REPORT_HTML_TEMPLATE);
            foreach (TestReportData reportData in data.testReportData)
            {
                string idFormattedModel = reportData.deviceModel.Replace(" ", "_");
                string deviceModelFormatted = AutomatedQaTools.SanitizeStringForUseInFilePath(idFormattedModel);
                string reportPath = Path.Combine(pathToReports, deviceModelFormatted).Replace("\\", "/");
                Directory.CreateDirectory(reportPath);
                string json = GetSignedUrlContents(reportData.reportDataSignedUrl);
                ReportingManager.GenerateHtmlReport(json, reportPath);
                sb.AppendLine($"<input id='{idFormattedModel}' class='test-results' type='hidden' value='{json}' />");
                sb.AppendLine($"<input id='{idFormattedModel}_url' class='test-path' type='hidden' value='{reportPath}' />");
            }
            string cloudReportPath = Path.Combine(pathToMultiReport, "cloud_test_run_report.html");
            File.WriteAllText(cloudReportPath, sb.ToString());
            return cloudReportPath;
        }

        private static void TestResultsToXML(TestResultsResponse data)
        {
            var tests = new List<ReportingManager.TestData>();
            foreach (var result in data.testResults)
            {
                foreach (var c in result.counters)
                {
                    var testData = new ReportingManager.TestData();
                    testData.TestName = $"{result.deviceModel}:{result.deviceName}:{result.testName}:{c._name}";
                    testData.Status = c._value == 1 ? ReportingManager.TestStatus.Pass.ToString() : ReportingManager.TestStatus.Fail.ToString();
                    tests.Add(testData);
                }
            }

            ReportingManager.GenerateXmlReport(tests, Path.Combine(CloudTestConfig.BuildFolder, "cloud-test-report.xml"));
        }
    }

    [Serializable]
    public class UploadUrlResponse
    {
        public string id;
        public string upload_uri;
    }

    [Serializable]
    public class CloudTestPayload
    {
        public string buildId;
        public List<string> testNames;
    }

    [Serializable]
    public class CloudTestPayloadWithDeviceSelection
    {
        public string buildId;
        public List<string> testNames;
        public string deviceSelectionInformation;

        public CloudTestPayloadWithDeviceSelection(CloudTestPayload ctp, string dsi)
        {
            this.buildId = ctp.buildId;
            this.testNames = ctp.testNames;
            this.deviceSelectionInformation = dsi;
        }
    }

    [Serializable]
    public class BundleUpload
    {
        public string buildPath;
        public string buildName;
    }

    [Serializable]
    public class JobStatusResponse
    {
        [SerializeField]
        public string jobId;
        public string status;

        public JobStatusResponse(string jobId, string status)
        {
            this.jobId = jobId;
            this.status = status;
        }

        public JobStatusResponse()
        {
            jobId = "";
            status = "UNKNOWN";
        }

        public bool IsInProgress()
        {
            return status != "COMPLETED" && status != "ERROR" && status != "UNKNOWN";
        }
    }

    [Serializable]
    public class TestCounter
    {
        public string _name;
        public int _value;
    }

    [Serializable]
    public class GetUploadURLPayload
    {
        public string name;
        public string description;
        public string buildType;
    }

    [Serializable]
    public class TestResult
    {
        public string testName;
        public string deviceModel;
        public string deviceName;
        public TestCounter[] counters;
    }

    [Serializable]
    public class TestReportData
    {
        public string deviceModel;
        public string deviceOS;
        public string reportDataSignedUrl;
    }

    [Serializable]
    public class TestResultsResponse
    {
        
        public TestReportData[] testReportData;
        public bool allPass;
        public TestResult[] testResults;
    }

    [Serializable]
    public class BuildInfo
    {
        public string buildName;
        public string buildId;
        public string createdAt;
    }

    [Serializable]
    public class GetBuildsResponse
    {
        public BuildInfo[] builds;
    }

    [Serializable]
    public class JobInfo
    {
        public string buildName;
        public string jobId;
        public string createdAt;
    }
    
    [Serializable]
    public class GetJobsResponse
    {
        public JobInfo[] jobs;
    }

    [Serializable]
    public class Log
    {
        public string url;
        public string deviceName;
    }

    public class GetLogResponse
    {
        public Log[] deviceLogs;
    }
    
    
}
