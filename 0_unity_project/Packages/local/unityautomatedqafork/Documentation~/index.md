# Automated QA 
The Automated QA (`com.unity.automated-testing`) package enables users to create Unity Tests from recordings of touch and drag interactions with the UI of a Unity Project and run Unity Tests on iOS and Android devices in our cloud device farm (hosted and managed by Unity) - tests can be executed from the Unity editor, command line, or your continuous integration (CI) pipeline.

## Development On Hold
December 6, 2021: Automated QA Development has been put on hold until further notice. Please refer to this [Forum post](https://forum.unity.com/threads/automated-qa-development-on-hold.1211454/), and continue to monitor our [Unity DevOps Roadmap](https://unity.com/roadmap/devops ) for more details. Thank you for your interest and support.

## Limitations: 
- Unity 2019.4 or above required

## Installation

1. With your project open in the Unity Editor, open the Package Manager (Window > Package Manager).
2. Press the plus button (`+`) in the top left of Package Manager and then select "Add package from git URL...".
3. Enter `com.unity.automated-testing` in the text box and then press "Add".

## Main Features
### [Recorded Playback](RecordedPlayback.md)
Record touch or drag input on Unity UI Elements to automate interaction with the UI of a Unity Project.

### [Recorded Testing](RecordedTesting.md)
Use recordings (recorded playbacks) to drive [Unity Test Framework Tests](https://docs.unity3d.com/Packages/com.unity.test-framework@latest) - in the editor, on a local device, or on iOS and Android devices in Unity's cloud device farm.

### [Cloud Device Testing from the Command Line (or CI pipeline)](CloudDeviceTestingCLI.md)
Run [Unity Test Framework Tests](https://docs.unity3d.com/Packages/com.unity.test-framework@latest) on Unity's cloud device farm from the command line or your continuous integration (CI) pipeline.


## Advanced/Experimental Features
### [Automators](Automators.md)
Create automated playthroughs from recording segments and C# scripts (e.g. cheat codes or bots).

### [Reporting](Reporting.md)
View an HTML report summarizing the results of each recorded playback or recorded test.

### Settings
Customizable variables/settings can be changed in the Settings editor window `(Automated QA Hub > Settings)`. These will be loaded and used by future test runs. 

Custom variables can be created and stored through this window and referenced in Automators using `AutomatedQASettings.Get[DataType]FromCustomSettings(string key)`.

