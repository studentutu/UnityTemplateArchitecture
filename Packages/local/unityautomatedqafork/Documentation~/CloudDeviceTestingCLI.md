# Running Cloud Device Tests From the Command Line (Access Controlled while Under Development)
Cloud testing can be initiated from the command line, which enables cloud testing from your continuous integration (CI) pipeline. 


## Prerequisites
See [Recorded Testing](RecordedTesting.md) for instructions on how to configure your project to run cloud device tests.

### Obtain an Access Token
Running cloud device tests requires a Unity access token for authentication to the device testing service. This token can be obtained using curl and passed in to the CI commands below using the `-token` parameter. Make sure to set your `$UNITY_USERNAME` and `$UNITY_PASSWORD` values before running this command.

```bash
AUTH_TOKEN=`curl --header "Content-Type: application/json" --request POST --data '{"username":"'$UNITY_USERNAME'","password":"'$UNITY_PASSWORD'","grant_type":"PASSWORD"}' https://api.unity.com/v1/core/api/login | grep access_token | cut -d '"' -f 4`
```

Alternatively if you would like to create a service account for authentication a python script is provided in the <automated-testing-package>/Scripts/ServiceAccountManager directory with instructions.

## Running Tests
To run tests on the Unity cloud device testing service from the command line, execute the `CloudTestBuilder.BuildAndRunTests` method which will create a new test build, upload it to our cloud service, run all tests with the [CloudTest] attribute, and return a Junit `cloud-test-report.xml` file in the `outputDir` with test results. Additionally, an HTML report with simple information on ALL tests run is generated. Finally, a detailed HTML run report is generated that shows extended information on all tests that utilize the Automated QA package's test run automation logic. 

```bash
Unity.exe -batchmode -nographics -quit -executeMethod Unity.CloudTesting.Editor.CloudTestBuilder.BuildAndRunTests -projectPath PATH_TO_YOUR_PROJECT -testPlatform Android -token $AUTH_TOKEN -projectId YOUR_PROJECT_ID
```

#### Available Command Line Arguments
##### `-token` 
Unity access token for authentication to the device testing service.

##### `-projectId` 
Unity project id, defaults to `Application.cloudProjectId` however this is only configured for projects with a logged in user.

##### `-testPlatform` 
The platform to run tests on. Accepted values are `Android` and `iOS`.

##### `-outputDir` (optional)
Directory used to store the generated test build and test result files, defaults to the [persistent data path](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html). 

## Other Commands

### Create Build

Creates a test build that can be uploaded to the cloud device testing service. Optionally, you can add the `-exportAsGoogleAndroidProject` flag to generate as a Gradle project if you need additional steps to build an Android APK. 

```bash
Unity.exe -batchmode -nographics -quit -outputDir "Builds" -executeMethod Unity.CloudTesting.Editor.CloudTestBuilder.CreateBuild -targetPlatform=TARGET_PLATFORM
```

#### Available Command Line Arguments

##### `-testPlatform` 
The platform to run tests on. Accepted values are `Android` and `iOS`.

##### `-outputDir` (optional)
Directory used to store the generated test build and test result files, defaults to the [persistent data path](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html). 

##### `-exportAsGoogleAndroidProject` (optional, Android only)
Generates a Gradle project that can be used to build the APK outside of Unity.


### Upload And Run Tests

Uploads a build to the cloud device testing service and run all tests with the [CloudTest] attribute. Note that this file must be a test build created by CloudTestBuilder.CreateBuild.

```bash
Unity.exe -batchmode -nographics -quit -executeMethod Unity.CloudTesting.Editor.CloudTestBuilder.UploadAndRunTests -uploadFile FILEPATH -token $AUTH_TOKEN -projectId YOUR_PROJECT_ID
```

#### Available Command Line Arguments

##### `-token` 
Unity access token for authentication to the device testing service.

##### `-projectId` 
Unity project id, defaults to `Application.cloudProjectId` however this is only configured for projects with a logged in user.

##### `-uploadFile` 
The path to your iOS .ipa or Android .apk test build to upload to the cloud device testing service. 
