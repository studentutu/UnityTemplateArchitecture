# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.8.1] - 2021-12-06
Automated QA Development has been put on hold until further notice. Please refer to this [Forum post](https://forum.unity.com/threads/automated-qa-development-on-hold.1211454/), and continue to monitor our [Unity DevOps Roadmap](https://unity.com/roadmap/devops ) for more details. Thank you for your interest and support.

## [0.8.0] - 2021-11-17

### Bug Fixes and Minor Changes
- Fixed issue where GameObjects and query strings with hyphens threw errors in Driver logic.
- Fix issue where the query selector was checking for a closing bracket too early.
- Updated namespaces to avoid the word `Editor` to prevent CS0118 errors.
- Fixed url to upload recordings for cloud device testing. 
- Fixed issue where the game crawler was not properly recording actions.
- Fixed bug where recording data gets cleared if the original entry scene is reloaded while recording.

## [0.7.0] - 2021-10-18

### Major Features and Improvements
- Added `RecordableInput` class that can be used as a drop in replacement for the [Input](https://docs.unity3d.com/ScriptReference/Input.html) class that works with recorded playback. Currently mouse clicks, touch, keyboard keys, and joystick buttons are supported. Pass-through methods are provided for the remaining functionality for ease of use.
- Added scriptable object CloudTestDeviceInput that lets a user specify specific devices on which to execute their tests in the cloud.
- Added support to retrieve HTML reports for cloud device runs
	- Added new report to aggregate display of results for multi-device cloud runs, with links to each individual device's run results in an existing HTML report.
- Added Heap size performance sampling to reporting charts found in the existing test report.
- Added Page Objects to test generation tool.
	- Every GameObject that code interacts with has its query string locator stored in a single Page Object class, referenced by any number of tests.
	- Page Object properties are organized in different Page Object classes based on the scene they are located in.
	- When generating tests, the tool will see if an existing Page Object property exists. It will only add a property to a class if no reference exists.
- All menu options moved to a single "Automated QA Hub" window
- Moving Cloud Testing behind a feature flag temporarily as we redesign the UX. Please email us at AutomatedQA@unity3d.com for early access."

### Bug Fixes and Minor Changes
- Added support for custom report data.
- Fixed error when generating full test from a recording that starts with a number.
- Added option to generate simpler code from the Test Generator by utilizing the Driver logic's new query string approach to finding GameObjects.
- Rewrote Recorded Playback editor window in new UIElement logic.
	- Redesigned look of window.
	- Added GameCrawler "Crawl" button to window, which executes a default crawl and generates a recording json file.
- Added tracking for the object index in the hierarchy to help differentiate objects with the same name
- Added fix to ensure automation waits for a GameObject animating from off screen to finish moving into the camera frustum before interacting with it.
- Added enforcement of an en-US culture in CodeGenerator logic.
- Added a context menu with the ability to generate tests in the main recorded playback window.
- Added a script for creating and managing service accounts to use in CI workflows.
- Fixed an issue where spaces in recording json file names were used when generating a class name in the Test Generator.
- Fixed an issue that causes backslashes to be used in generated code for Staged Runs.
- Added "TryPerform" to Driver class. If an action cannot be performed, the test will not fail.
- Added drop downs for builds and jobs in cloud test window
- Added GameElement "AutoID" - an automatically-generated GUID to uniquely identify GameElements.
	- Adds GameElement script to all interactable GameObjects and assigns a unique GUID automatically.
	- Allows for GameObject to be renamed, moved, etc. without breaking a test that references this ID using Driver.cs.
- Added menu option to automatically add GameElements to interactable UI Elements in a scene. Menu -> "Automated QA" -> "Add Game Elements To Scene Objects"
- Renamed AutomatedQAEditorSettings to AutomatedQABuildConfig and AutomatedQARuntimeSettings to AutomatedQASettings.
- Fixed an issue generating iOS cloud testing builds when there are spaces in the build name.

### Breaking changes
- Removed "Full Build" cloud testing support.

## [0.6.1] - 2021-08-11

### Major Features and Improvements
- Added experimental Staged Run feature for grouped test generation. (Assets > Create > Automated QA > Experimental > Staged Run)
- New GameElement and ElementQuery classes for reliably finding GameObjects, no matter where they are moved in a scene.
	- Uses Xpath & JQuery-like query string selectors for identifying GameObjects.
- New Driver class that allows for much simpler ways to write custom code and perform actions on objects (ex: `Driver.Perform.Click("#SubmitButton")`).
- Added GameCrawler that allows users to create an AutomatedRun that plays randomly through a game, recording warnings, errors, and notifying users when and where it gets stuck.

### Bug Fixes and Minor Changes
- Reverting change that removed the ability to generate "Simple Tests", which is a test that points to a recording file.
- Fixed an issue with temp directories not being properly deleted.
- Adding FPS tracking to reports. FPS sampled over duration of test execution is displayed in a graph accessible from the html report (FPS Data button).
- Fixed issue allowing objects off screen to be interacted with.
- Add support for recording right and middle mouse clicks.
- Added functionality to allow both recording and playback while editor is in play mode.
	- Both record & playback can be done repeatedly in the same session.
	- Editor does not stop playmode on completion, but both record & playback can still be started outside of playmode.
- Added support for running cloud tests from the command line using `BuildAndRunTests`.
- Fixed issue where tests may timeout during command line batch mode runs.
- Fixed issue with the InputField text listener not being added right away.
- Screenshot timing after each playback action completes is now configurable in settings.
- Exposed "Quit on finish" in Automated Run Config.
- Added ability to generate a "Full" test from Automated Runs.
- Added settings for log level.
- Added support for TextMeshPro TMP_InputFields and TMP_Text fields
	- Can record and playback input into TMP_InputFields, and find objects in ElementQuery by TMP_InputField & TMP_Text text content.

### Breaking changes
- Several APIs were moved from the test base class to Driver.cs. "Full" generated tests from v0.5.0 will need their PerformAction and RegisterStep method invocations updated.
- The experimental Composite Recordings window has been removed. Instead, Automated Runs can be used to playback multiple recordings in sequence. 

## [0.5.0] - 2021-06-24

### Major Features and Improvements
- Support for running tests on real iOS devices in the cloud
- New [CloudTest] attribute can be used to specify any Unity Test Framework test to run on real devices in the cloud
- `Automated QA > Test Generation...` allows for step-by-step Unity Test Framework test generation (C# code).
	- Generates code for every step/action taken in a recording.
	- Allows for assertions or additional custom logic between each step.
	- Can select which recordings to generate tests for.
	- Editor Window warns user if about to overwrite custom edits in a recording's test when re-generating test.	

### Breaking changes
- Recording file in RecordedPlaybackAutomator is now an asset reference instead of path string
  - Migration: Update the file path in AutomatedRuns using RecordedPlaybackAutomator

### Bug Fixes and Minor Changes
- Added dynamic wait logic so that differences in load times or animations does not result in automation failing to interact with the target object.
- Delayed loading of the recorded playback controller to avoid issues with initializing too early.

## [0.4.0] - 2021-06-02

### Major Features and Improvements
- HTML & XML reports for AutomatedQA runs.
  - XML report is designed for loading tests results into a CI process run.
  - Latest HTML reportcan be opened in editor from the "Recorded Playback" and "Composite Recordings" editor windows after playback.
  - Both currently stored in Application.persistentDataPath. CI/CD must copy report from path (Will be integrated with Cloud services & modified to take raw test data from devices so that explicit file extraction is not necessary).
- Added settings file for editable values used in Automated Qa package, thus making various AutomatedQA behaviors customizable (more to be added as time goes on).
  - Added new editor window to edit the settings file (Automated QA > Settings).
  - Multiple config files with varying values can be stored in `Assets/AutomatedQA/Resources`, and desired settings can be requested from cloud config via file name.
  
### Bug Fixes and Minor Changes
- Added placeholder segment file names in Composite Recording window when selecting "Save Segment".
- Updated VisualFx to prevent visual clutter from a rapid series of events.
- Updated validation of GameObject that is about to be clicked. Increased performance. Now checks that an object is off screen or under another object that would intercept the click.
- Updated recorded tests to load an empty scene after execution.
- Fixed ignoring depth surface warnings when taking screenshots on Mac.
- Added new Automators: Scene Automator and Text Input Automator.
- Fixed event timings and scene loading with composite recordings

## [0.3.1] - 2021-05-04

### Major Features and Improvements
- New `AutomatedRun` object to link together recordings and custom C# scripts to automate gameplay. 
  - Create an AutomatedRun with `Create -> Automated QA -> Automated Run`
- New `Automator` class. Extend this class to create custom automators
  - Extend the `AutomatorConfig` class for your Automator to expose it in the `AutomatedRun` inspector.
 
### Bug Fixes and Minor Changes
- Fixed an issue where drop events had extra delay
- Fixed an issue with the Upload Recording window requiring entitlements
- Removed dependency on com.unity.nuget.newtonsoft-json package
- Fixed entry scene to work with initialization scenes
- Moved menu items to top level Automated QA menu
- Added fix to CloudTestResults.cs that prevents ITestRunCallback from being eagerly stripped by the "ahead of time" compilation in IL2CPP builds.
- Added logic to cleanup temporary files accumulating from previous recordings and stored in the persistent data path.
- Removed Linq usage from package as Linq can be a heavy library for mobile game development.

## [0.3.0] - 2021-04-21

### Breaking changes
- Simplified assembly definitions 
  - Migration: Please update asmdefs to reference Unity.AutomatedQA
- Renamed asset directory to AutomatedQA 
  - Migration: Please delete the old AutomatedTesting folder
  
### Bug Fixes and Minor Changes
- Package directory restructure
- Added SettingsManager package and AutomatedQAEditorSettings/AutomatedQARuntimeSettings to wrap package settings
- Disabled Cloud Testing window for unsupported platforms
- Fixed an issue with recorded tests using composite recordings
- Fixed an issue causing two simultaneously active EventSystem components, resulting in a flood of console warnings
- Fixed an issue where multi-clicking "Record" or "Play" buttons creates multiple RecordedPlaybackController GameObjects

## [0.2.0] - 2021-04-05

### Major Features and Improvements
- Added new window under Window > Automated Testing > Composite Recordings that allows:
  1) Multiple recordings to be combined together via the UI
  2) Multiple recordings to be captured continuously during record mode
- Added UIElements to composite recordings window
- Update the package display name to Automated QA
- Object interactions now use the original location inside the object during playback

### Bug Fixes and Minor Changes
- Fixed bug where Record mode is automatically enabled on window open
- 2 new window paths: 
  1) Window > Automated Testing > Recorded Playback and
  2) Window > Automated Testing > Advanced > Composite Recorded Playback

## [0.1.0] - 2021-03-01

### Major Features and Improvements
- Can now start recordings by clicking Record or Play in the Recorded Playback window without the need to manually add a RecordedInputModule to the scene.
- Add edit button to rename recordings 

### Bug Fixes and Minor Changes
- Fixed import errors in Unity Editor 2018.4.18f1+. Note: At this time we do not officially support Unity versions less than 2019.4.
- Fix bug where Generated Recorded Tests are created in the wrong directory 
- Fix bug where object presses are sometimes not properly played back 

## [0.0.7] - 2021-02-18

### Major Features and Improvements
- Generate Recorded Tests
  - Menu: Tools > Automated Testing > Generate Recorded Tests
  - Does not work with recording data created before version 0.0.7
- Recorded playback analytics events have been added

### Known Issues
- Very quick presses of buttons in recordings do not play back correctly. Workaround: hold the click for about a second.

### Bug Fixes and Minor Changes
- Pretty print recording JSON data

## [0.0.6-preview.1] - 2021-02-12
- Fixed documentation structure

## [0.0.5-preview.1] - 2021-02-11
- Updated package dependencies

## [0.0.4-preview.1] - 2021-02-08
- Split experimental features out to com.unity.automated-testing.experiments

## [0.0.3-preview.1] - 2021-02-08
- Restructured package directories and updated documentation

## [0.0.2-preview.1] - 2021-01-29
- Merged recorded playback and cloud testing packages

## [0.0.1-preview.1] - 2021-01-14
- Support for running UTF tests using recordings

## [0.0.0-preview.2] - 2020-11-16
- Use object references for recording playback actions

## [0.0.0-preview.1] - 2020-10-06
- Initial version
