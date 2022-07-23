using System;
using NUnit.Framework;
using Unity.CloudTesting.Editor;
using UnityEditor;

namespace TestPlatforms.Cloud
{
    public class CloudTestBuilderTests
    {
        private string testOutputDir = "/test/out";
        private string testAccessToken = "fakeToken";
        private string testProjectId = "fakeProjectId";
        private BuildTarget testPlatform = BuildTarget.Android;

        private MockCloudTestClient mockClient = new MockCloudTestClient();
        private ICloudTestClient originalClient;

        [SetUp]
        public void Setup()
        {
            originalClient = CloudTestBuilder.Client;
            CloudTestBuilder.Client = mockClient;
        }

        [TearDown]
        public void TearDown()
        {
            CloudTestBuilder.Client = originalClient;
        }

        [Test]
        public void ParseCommandLineArgs_SucceedsWithSeparateArgs_UnitTest()
        {
            string[] testArgs = {"-token", testAccessToken, "-outputDir", testOutputDir, "-testPlatform", testPlatform.ToString(), "-projectId", testProjectId};

            var commandLineArgs = CloudTestBuilder.ParseCommandLineArgs(testArgs);

            Assert.AreEqual(commandLineArgs.AccessToken, testAccessToken);
            Assert.AreEqual(commandLineArgs.TargetPlatform, testPlatform);
            Assert.AreEqual(CloudTestConfig.BuildFolder, testOutputDir);
        }

        [Test]
        public void ParseCommandLineArgs_SucceedsWithEquals_UnitTest()
        {
            string[] testArgs = {$"-token={testAccessToken}", $"-outputDir={testOutputDir}", $"-testPlatform={testPlatform}", $"-projectId={testProjectId}"};

            var commandLineArgs = CloudTestBuilder.ParseCommandLineArgs(testArgs);

            Assert.AreEqual(commandLineArgs.AccessToken, testAccessToken);
            Assert.AreEqual(commandLineArgs.TargetPlatform, testPlatform);
            Assert.AreEqual(CloudTestConfig.BuildFolder, testOutputDir);
        }
        
        [Test]
        public void ParseCommandLineArgs_ErrorsWithInvalidPlatform_UnitTest()
        {
            string[] testArgs = {"-testPlatform", "FakePlatform"};

            Assert.Throws<Exception>(() => CloudTestBuilder.ParseCommandLineArgs(testArgs));
        }

        [Test]
        public void AwaitTestResults_SucceedsWithPassingTests_UnitTest()
        {
            mockClient.mockTestResults.allPass = true;
            
            var results = CloudTestBuilder.AwaitTestResults("fakeJobId", testAccessToken, testProjectId);

            Assert.True(results.allPass);
        }

        [Test]
        public void AwaitTestResults_ErrorsWithFailingTests_UnitTest()
        {
            mockClient.mockTestResults.allPass = false;

            Assert.Throws<Exception>(() => CloudTestBuilder.AwaitTestResults("fakeJobId", testAccessToken, testProjectId));
        }
    }
}