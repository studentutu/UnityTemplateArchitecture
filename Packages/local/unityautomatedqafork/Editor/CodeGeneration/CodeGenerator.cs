using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using static UnityEngine.EventSystems.RecordingInputModule;
using Unity.RecordedPlayback;
using System.Globalization;

namespace Unity.AutomatedQA.Editor
{
    public static class CodeGenerator
    {
        private static CultureInfo currentCulture;
        private static string GeneratedTestsDirectory => Path.Combine(Application.dataPath, AutomatedQASettings.PackageAssetsFolderName, "GeneratedTests");
        private static string GeneratedTestsFolderName => "GeneratedTests";
        private static string GeneratedTestsAssemblyName => "GeneratedTests.asmdef";
        private static string ScriptTemplatePath = "Packages/com.unity.automated-testing/Editor/ScriptTemplates/";
        private static string GeneratedTestAssemblyTemplatePath => $"{ScriptTemplatePath}Assembly Definition-GeneratedTests.asmdef.txt";
        private static string GeneratedTestScriptTemplatePath => $"{ScriptTemplatePath}C# Script-GeneratedRecordedTests.cs.txt";
        private static string GeneratedAutomationTestscriptTemplatePath =>
            $"{ScriptTemplatePath}C# Script-GeneratedAutomatedRunTests.cs.txt";

        private static (string parent, List<string> segments) NestedSegmentHierarchy;
        public static readonly string NEW_LINE = "\r\n";

        static string indentOne, indentTwo, indentThree, indentFour;
        static CodeGenerator()
        {
            //Formatting indentations for appended code. Each is a multiple of 4 spaces (1 tab).
            indentOne = GetIndentationString(1);
            indentTwo = GetIndentationString(2);
            indentThree = GetIndentationString(3);
            indentFour = GetIndentationString(4);
        }

        public static List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)> GenerateTestFromAutomatedRun(string automatedRunFileName, bool ignoreDiffs, bool isSimpleTest, bool useSimplifiedDriverCode, string stepFileToOverwrite = "")
        {
            currentCulture = CultureInfo.DefaultThreadCurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");

            // Create required files for UnityTest script creation.
            CreateTestAssemblyFolder();
            CreateTestAssembly();

            bool isStepFileOverwrite = !string.IsNullOrEmpty(stepFileToOverwrite);
            if (!automatedRunFileName.EndsWith(".asset"))
                automatedRunFileName += ".asset";
            string className = $"Tests_{GetClassNameForRecording(automatedRunFileName)}";

            // If this is a "simple" generated test, simply check if edits have been made to the file and return or overwrite accordingly.
            string cSharpFileName = $"{className}.cs";
            // For full tests, continue.
            List<(string assetFileName, string cSharpScriptFileName, bool isStepFile)> filesWithEdits = new List<(string assetFileName, string cSharpScriptFileName, bool isStepFile)>();
            string stepFilesDirectory = Path.Combine(GeneratedTestsDirectory, "Steps");
            if (!Directory.Exists(stepFilesDirectory))
            {
                Directory.CreateDirectory(stepFilesDirectory);
            }

            // Get recording automated run asset
            AutomatedRun automatedRun = AssetDatabase.LoadAssetAtPath<AutomatedRun>(automatedRunFileName);

            string stepFileClassName, stepFileCSharpName, lastTouchDataFile, lastJsonFileName;
            stepFileClassName = stepFileCSharpName = lastTouchDataFile = lastJsonFileName = string.Empty;
            List<string> touchIds = new List<string>();
            List<string> handledFiles = new List<string>();
            List<(string Scene, string PropertyName, string QuerySelector, string ObjectHierarchyPath)> pageObjectProperties = new List<(string, string, string, string)>();
            StringBuilder setUpLogic = new StringBuilder();
            StringBuilder stepFile = new StringBuilder();
            StringBuilder touchDataListForTestWithSimplifiedDriverLogic = new StringBuilder();
            StringBuilder touchDataListForTestWithoutSimplifiedDriverLogic = new StringBuilder();
            StringBuilder simpleScript = new StringBuilder(GetAutomatedRunTestScript(automatedRunFileName));
            bool anyStepFilesGenerated = false;

            int automatorIndex = 0;
            InputModuleRecordingData recordingToUseForBaselineAspectAndResolution = null;
            foreach (AutomatorConfig automatedConfig in automatedRun.config.automators)
            {
                List<(string file, TouchData data)> steps = new List<(string file, TouchData data)>();

                if (automatedConfig.AutomatorType != typeof(RecordedPlaybackAutomator))
                {
                    string automatorLogic = $"{indentThree}yield return RunAutomator(typeof({automatedConfig.AutomatorType}), {automatorIndex++});";
                    touchDataListForTestWithSimplifiedDriverLogic.AppendLine(automatorLogic);
                    touchDataListForTestWithoutSimplifiedDriverLogic.AppendLine(automatorLogic);
                    continue;
                }

                if (((RecordedPlaybackAutomatorConfig)automatedConfig).recordingFile == null)
                {
                    Debug.LogError($"A text asset with a valid recording file is not set on the automator \"{automatedRun.name}\"");
                    continue;
                }
                string recordingFileJson = ((RecordedPlaybackAutomatorConfig)automatedConfig).recordingFile.text;
                InputModuleRecordingData testData = JsonUtility.FromJson<InputModuleRecordingData>(((RecordedPlaybackAutomatorConfig)automatedConfig).recordingFile.text);

                if (recordingToUseForBaselineAspectAndResolution == null)
                {
                    recordingToUseForBaselineAspectAndResolution = testData;
                }

                lastJsonFileName = ((RecordedPlaybackAutomatorConfig)automatedConfig).recordingFile.name;
                lastJsonFileName = GetClassNameForRecording(lastJsonFileName);
                stepFileClassName = $"Steps_{lastJsonFileName}";
                TouchData playbackComplete = testData.touchData.Last();
                string assetFilePath = Path.Combine(Application.dataPath, lastJsonFileName);

                // Add main recording file's TouchData actions (if any).
                foreach (TouchData data in testData.touchData)
                {
                    if (!testData.recordings.Any() && data.emitSignal != "playbackComplete")
                        steps.Add((assetFilePath, data));
                }

                // If this recording has child segments, add their TouchData actions to the list of steps.
                if (!isStepFileOverwrite && testData.recordings.Any())
                {
                    NestedSegmentHierarchy = (assetFilePath, new List<string>());
                    steps.AddRange(AddTouchDataFromSegments(testData.recordings, assetFilePath));
                    steps.Add((assetFilePath, playbackComplete));
                }

                int index = 0;
                foreach ((string file, TouchData data) touchData in steps)
                {

                    // Create a step file for each recording file.
                    if (!isSimpleTest && !handledFiles.Contains(stepFileClassName))
                    {
                        // Finish previous file.
                        if (handledFiles.Any())
                        {
                            stepFile.AppendLine($"{indentTwo}}}");
                            stepFile.AppendLine($"{indentOne}}}");
                            stepFile.AppendLine($"}}");
                            if (!isSimpleTest)
                                CreateStepFile(lastTouchDataFile, stepFileCSharpName, stepFile, ignoreDiffs, stepFileToOverwrite, ref filesWithEdits);
                        }

                        // Format recording name to remove characters that are invalid for use in a C# class name.
                        stepFileCSharpName = $"{stepFileClassName}.cs";
                        lastTouchDataFile = lastJsonFileName;
                        lastTouchDataFile += lastTouchDataFile.EndsWith(".json") ? string.Empty : ".json";
                        handledFiles.Add(stepFileClassName);
                        stepFile = new StringBuilder();
                        touchIds = new List<string>();

                        // Start new step file. Steps will be references in the test file.
                        stepFile.AppendLine("using System.Collections.Generic;");
                        stepFile.AppendLine("using UnityEngine;");
                        stepFile.AppendLine($"using static UnityEngine.EventSystems.RecordingInputModule;{NEW_LINE}");
                        stepFile.AppendLine("namespace GeneratedAutomationTests");
                        stepFile.AppendLine("{"); // Start namespace bracket.
                        stepFile.AppendLine($"{indentOne}/// <summary>{NEW_LINE}" +
                                            $"{indentOne}/// This segment touch data were generated by Unity Automated QA for the recording from the Assets folder at \"{Path.Combine(AutomatedQASettings.RecordingFolderName, lastTouchDataFile)}\"{NEW_LINE}" +
                                            $"{indentOne}/// You can regenerate this file from the Unity Editor Menu: Automated QA > Generate Recorded Tests{NEW_LINE}" +
                                            $"{indentOne}/// </summary>");
                        stepFile.AppendLine($"{indentOne}public static class {stepFileClassName}");
                        stepFile.AppendLine($"{indentOne}{{");
                        stepFile.AppendLine($"{indentTwo}public static Dictionary<string, TouchData> Actions = new Dictionary<string, TouchData>();");
                        stepFile.AppendLine($"{indentTwo}static {stepFileClassName}()");
                        stepFile.AppendLine($"{indentTwo}{{");
                    }

                    // Handle generation of a C# TouchData object, which is a 1-to-1 correlation with TouchData stored in the json recording file.
                    string waitSignal = string.IsNullOrEmpty(touchData.data.waitSignal) ? string.Empty : touchData.data.waitSignal;
                    string emitSignal = string.IsNullOrEmpty(touchData.data.emitSignal) ? string.Empty : touchData.data.emitSignal;
                    string objectName = string.IsNullOrEmpty(touchData.data.objectName) ? string.Empty : touchData.data.objectName;
                    string objectTag = string.IsNullOrEmpty(touchData.data.objectTag) ? string.Empty : touchData.data.objectTag;
                    string objectHierarchy = string.IsNullOrEmpty(touchData.data.objectHierarchy) ? string.Empty : touchData.data.objectHierarchy;
                    string idName = (string.IsNullOrEmpty(touchData.data.objectName) ? ( // Use the object name as the first choice in generating an id for the touch data.
                                        string.IsNullOrEmpty(touchData.data.objectTag) ? ( // Use the object tag as the second choice in generating an id for the touch data.
                                            string.IsNullOrEmpty(waitSignal) ? ( // Use the wait signal as the third choice in generating an id for the touch data.
                                                string.IsNullOrEmpty(emitSignal) ? // Use the emit signal as the fourth choice in generating an id for the touch data.
                                                    "Signal" : // If all other fields are empty (which they shouldn't be), then use this generic word in generating an id for the touch data.
                                                    emitSignal
                                            ) :
                                            waitSignal
                                        ) :
                                        touchData.data.objectTag
                                    ) :
                                    touchData.data.objectName).Replace(" ", "_");
                    string touchIdBase = $"{(touchData.data.eventType == TouchData.type.none ? "EMIT" : touchData.data.eventType.ToString().ToUpper())}_{idName}";
                    string touchIdFinal = touchIdBase;
                    int idIndex = 2;
                    bool uniqueId = false;
                    while (!uniqueId)
                    {
                        if (touchIds.Contains(touchIdFinal))
                        {
                            touchIdFinal = $"{touchIdBase}_{idIndex}";
                            idIndex++;
                        }
                        else
                        {
                            touchIds.Add(touchIdFinal);
                            uniqueId = true;
                        }
                    }

                    stepFile.AppendLine($"{indentThree}Actions.Add(\"{touchIdFinal}\", new TouchData{NEW_LINE}" +
                                             $"{indentThree}{{{NEW_LINE}" +
                                             $"{indentFour}pointerId = {touchData.data.pointerId},{NEW_LINE}" +
                                             $"{indentFour}eventType = TouchData.type.{touchData.data.eventType},{NEW_LINE}" +
                                             $"{indentFour}timeDelta = {touchData.data.timeDelta}f,{NEW_LINE}" +
                                             $"{indentFour}position =  new Vector3({touchData.data.position.x}f, {touchData.data.position.y}f),{NEW_LINE}" +
                                             $"{indentFour}positional = {touchData.data.positional.ToString().ToLower()},{NEW_LINE}" +
                                             $"{indentFour}scene = \"{(string.IsNullOrEmpty(touchData.data.scene) ? testData.entryScene : touchData.data.scene)}\",{NEW_LINE}" +
                                             $"{indentFour}waitSignal = \"{waitSignal}\",{NEW_LINE}" +
                                             $"{indentFour}emitSignal = \"{emitSignal}\",{NEW_LINE}" +
                                             $"{indentFour}keyCode = \"{touchData.data.keyCode}\",{NEW_LINE}" +
                                             $"{indentFour}inputDuration = {touchData.data.inputDuration}f,{NEW_LINE}" +
                                             $"{indentFour}inputText = \"{touchData.data.inputText}\",{NEW_LINE}" +
                                             $"{indentFour}querySelector = \"{touchData.data.querySelector}\",{NEW_LINE}" +
                                             $"{indentFour}objectName = \"{objectName}\",{NEW_LINE}" +
                                             $"{indentFour}objectTag = \"{objectTag}\",{NEW_LINE}" +
                                             $"{indentFour}objectHierarchy = \"{objectHierarchy}\",{NEW_LINE}" +
                                             $"{indentFour}objectOffset =  new Vector3({touchData.data.objectOffset.x}f, {touchData.data.objectOffset.y}f){NEW_LINE}" +
                                             $"{indentThree}}});");

                    string testStep = string.Empty;
                    if (touchData.data.eventType != TouchData.type.release)
                    {
                        /*
                        * Code will register each step file step used by a test in that test's SetUpClass method.
                        * This is because recording logic expects all TouchData to be set from the start of playback, as opposed to adding TouchData with each action we invoke.
                        */
                        string propertyName = string.Empty;
                        string methodNameOfAction = GetDriverMethodNameBasedOnActionType(touchData.data.eventType);
                        string queryString = DetermineBestQueryStringToFindGameObjectWith(touchData.data);

                        string elementType = string.Empty;
                        propertyName = queryString.ToCharArray()[0] == '#' ? queryString.Substring(1, queryString.Length - 1) : $"{(string.IsNullOrEmpty(touchData.data.scene) ? testData.entryScene : touchData.data.scene)}_{touchData.data.objectName}";
                        switch (touchData.data.eventType)
                        {
                            case TouchData.type.drag:
                                elementType = "Draggable";
                                break;
                            case TouchData.type.input:
                                elementType = "Input";
                                break;
                            case TouchData.type.press:
                                elementType = "Clickable";
                                break;
                            default:
                                elementType = "Element";
                                break;
                        }
                        propertyName = $"{elementType}_{propertyName.SanitizeStringForUseInGeneratingCode()}";
                        string pageObjectClassName = $"Scene_{(string.IsNullOrEmpty(touchData.data.scene) ? testData.entryScene : touchData.data.scene)}_PageObject";

                        // If this is a drag event, we need to provide the drag object and the target.
                        if (touchData.data.eventType == TouchData.type.drag)
                        {
                            int indexRelease = steps.FindIndex(x => x == touchData) + 1;
                            if (indexRelease >= steps.Count)
                                throw new UnityException("A drag event was found in the recording that did not have a corresponding release event.");

                            string target = string.Empty;
                            // If target is not a GameObject, use a coordinate position.
                            if (string.IsNullOrEmpty(steps[indexRelease].data.objectName))
                            {
                                target = $"new Vector2({Math.Round(steps[indexRelease].data.position.x, 1)}f, {Math.Round(steps[indexRelease].data.position.y, 1)}f)";
                            }
                            else
                            {
                                target = $"\"{DetermineBestQueryStringToFindGameObjectWith(steps[indexRelease].data)}\"";

                            }
                            testStep = $"{indentThree}yield return Driver.Perform.Drag({pageObjectClassName}.{propertyName}, {target}, {Math.Round(steps[indexRelease].data.timeDelta, 1)}f);";
                        }
                        else if (touchData.data.eventType == TouchData.type.input)
                        {
                            testStep = $"{indentThree}yield return Driver.Perform.SendKeys({pageObjectClassName}.{propertyName}, \"{touchData.data.inputText}\", {Math.Round(touchData.data.inputDuration, 1)}f);";
                        }
                        else
                        {
                            testStep = $"{indentThree}yield return Driver.Perform.{methodNameOfAction}({pageObjectClassName}.{propertyName});";
                        }
                        touchDataListForTestWithSimplifiedDriverLogic.AppendLine(testStep);

                        // Handle creation of PageObject(s) so that references to query strings are used in tests, rather than the query selectors themselves (makes it easier to maintain and edit automation code).
                        if (!string.IsNullOrEmpty(queryString) && useSimplifiedDriverCode && (
                            touchData.data.eventType == TouchData.type.input ||
                            touchData.data.eventType == TouchData.type.drag ||
                            touchData.data.eventType == TouchData.type.press))
                        {
                            pageObjectProperties.Add((string.IsNullOrEmpty(touchData.data.scene) ? testData.entryScene : touchData.data.scene, propertyName, queryString, touchData.data.GetObjectScenePath()));
                        }
                    }

                    // For each step, add an extra line before the next TouchData object in our generated code.
                    if (index != testData.touchData.Count - 1)
                        stepFile.AppendLine(string.Empty);

                    setUpLogic.AppendLine($"{indentThree}Driver.Perform.RegisterStep({stepFileClassName}.Actions[\"{touchIdFinal}\"]);");
                    testStep = $"{indentThree}yield return Driver.Perform.Action({stepFileClassName}.Actions[\"{touchIdFinal}\"]); " + (touchData.data.eventType == TouchData.type.none ? $"// Emit {touchData.data.emitSignal}" :
                                $"// Do a \"{touchData.data.eventType}\" action " +
                                (string.IsNullOrEmpty(touchData.data.objectName) ? $"at \"{Math.Round(touchData.data.position.x, 2)}x {Math.Round(touchData.data.position.y, 2)}y\" coordinates " : $"on \"{(string.IsNullOrEmpty(touchData.data.objectHierarchy) ? touchData.data.objectName : $"{touchData.data.objectHierarchy}/{touchData.data.objectName}")}\"") +
                                $"in scene \"{(string.IsNullOrEmpty(touchData.data.scene) ? testData.entryScene : touchData.data.scene)}\".");
                    touchDataListForTestWithoutSimplifiedDriverLogic.AppendLine(testStep);

                    index++;
                    anyStepFilesGenerated = true;
                }
            }

            if (!useSimplifiedDriverCode && anyStepFilesGenerated)
            {
                // Finish final step file.
                stepFile.AppendLine($"{indentTwo}}}");
                stepFile.AppendLine($"{indentOne}}}");
                stepFile.AppendLine($"}}");
                if (!isSimpleTest)
                    CreateStepFile(lastJsonFileName, stepFileCSharpName, stepFile, ignoreDiffs, stepFileToOverwrite, ref filesWithEdits);
            }

            /*
                Generate test file with step-by-step action invocations generated previously.
                Generate two different versions for the sake of comparing all possible unedited version of a test to guarantee customer edits are not overwritten.
            */
            StringBuilder universalCode = new StringBuilder();
            StringBuilder fullScriptWithoutSimplifiedDriverCode = new StringBuilder();
            StringBuilder fullScriptWithSimplifiedDriverCode = new StringBuilder();
            universalCode.AppendLine($"using System.Collections;" +
                                  $"using UnityEngine;{NEW_LINE}" +
                                  $"using UnityEngine.TestTools;{NEW_LINE}" +
                                  $"using Unity.AutomatedQA;{NEW_LINE}" +
                                  $"using Unity.CloudTesting;{NEW_LINE}" +
                                  $"{NEW_LINE}namespace GeneratedAutomationTests{NEW_LINE}" +
                                  $"{{{NEW_LINE}" + // Start namespace bracket.
                                  $"{indentOne}/// <summary>{NEW_LINE}" +
                                  $"{indentOne}/// These tests were generated by Unity Automated QA for the automated run \"{automatedRunFileName}\".{NEW_LINE}" +
                                  $"{indentOne}/// You can regenerate this file from the Unity Editor Menu: Automated QA > Generate Recorded Tests{NEW_LINE}" +
                                  $"{indentOne}/// </summary>{NEW_LINE}" +
                                  $"{indentOne}public class {className} : AutomatedQATestsBase{NEW_LINE}" +
                                  $"{indentOne}{{{NEW_LINE}" + // Start class bracket.
                                  $"{indentTwo}/// Generated from recording file: \"{automatedRunFileName}\".{NEW_LINE}" +
                                  $"{indentTwo}[UnityTest]{NEW_LINE}" +
                                  $"{indentTwo}[CloudTest]{NEW_LINE}" +
                                  $"{indentTwo}public IEnumerator CanPlayToEnd(){NEW_LINE}" +
                                  $"{indentTwo}{{");

            fullScriptWithSimplifiedDriverCode.Append(universalCode.ToString());
            fullScriptWithSimplifiedDriverCode.Append(touchDataListForTestWithSimplifiedDriverLogic.ToString());
            fullScriptWithSimplifiedDriverCode.Append($"{indentTwo}}}{NEW_LINE}");
            fullScriptWithoutSimplifiedDriverCode.Append(universalCode.ToString());
            fullScriptWithoutSimplifiedDriverCode.Append(touchDataListForTestWithoutSimplifiedDriverLogic.ToString());
            fullScriptWithoutSimplifiedDriverCode.Append($"{indentTwo}}}{NEW_LINE}");

            fullScriptWithoutSimplifiedDriverCode.AppendLine($"{NEW_LINE}{indentTwo}// Steps defined by recording.{NEW_LINE}" +
                                $"{indentTwo}protected override void SetUpTestClass(){NEW_LINE}" +
                                $"{indentTwo}{{{NEW_LINE}" +
                                $"{setUpLogic}" +
                                $"{indentTwo}}}");

            universalCode = new StringBuilder();
            universalCode.AppendLine($"{NEW_LINE}{indentTwo}// Initialize test run data.{NEW_LINE}" +
                                  $"{indentTwo}protected override void SetUpTestRun(){NEW_LINE}" +
                                  $"{indentTwo}{{{NEW_LINE}" +
                                  $"{indentThree}automatedRun = GetAutomatedRun($\"{automatedRunFileName}\", \"{GetClassNameForRecording(automatedRunFileName)}\");{NEW_LINE}" +
                                  (recordingToUseForBaselineAspectAndResolution != null ?
                                     $"{indentThree}Test.entryScene = {(!isSimpleTest && pageObjectProperties.Any() ? $"Scene_{pageObjectProperties.First().Scene}_PageObject.SceneName" : $"\"{recordingToUseForBaselineAspectAndResolution.entryScene}\"")};{NEW_LINE}" +
                                     $"{indentThree}Test.recordedAspectRatio = new Vector2({recordingToUseForBaselineAspectAndResolution.recordedAspectRatio.x}f,{recordingToUseForBaselineAspectAndResolution.recordedAspectRatio.y}f);{NEW_LINE}" +
                                     $"{indentThree}Test.recordedResolution = new Vector2({recordingToUseForBaselineAspectAndResolution.recordedResolution.x}f,{recordingToUseForBaselineAspectAndResolution.recordedResolution.y}f);{NEW_LINE}"
                                     : string.Empty) +
                                  $"{indentTwo}}}{NEW_LINE}" +
                                  $"{indentOne}}}{NEW_LINE}" + // End class bracket.
                                  $"}}"); // End namespace bracket.

            fullScriptWithSimplifiedDriverCode.Append(universalCode.ToString());
            fullScriptWithoutSimplifiedDriverCode.Append(universalCode.ToString());

            string saveTestFilePath = string.Empty;
            bool fileExists = DoesFileExistedSomewhereInGeneratedCodeFolder(cSharpFileName);
            if (fileExists)
                saveTestFilePath = FindFileSomewhereInGeneratedCodeFolder(cSharpFileName).FileLocation;
            else
                saveTestFilePath = Path.Combine(GeneratedTestsDirectory, cSharpFileName);
            // Compare new generated content to old generated content. If there is a difference, the user has made edits and we want to confirm that the user wishes to overwrite them.
            if (!isStepFileOverwrite && !ignoreDiffs && DoesFileExistedSomewhereInGeneratedCodeFolder(cSharpFileName))
            {
                if (HasGeneratedTestContentBeenEdited(cSharpFileName, fullScriptWithSimplifiedDriverCode.ToString(), fullScriptWithoutSimplifiedDriverCode.ToString(), simpleScript.ToString()))
                {
                    filesWithEdits.Add((automatedRunFileName, cSharpFileName, false));
                }
            }

            bool generateScript =
                !isStepFileOverwrite // Ignore the test file if we are only overwriting step files.
                && (ignoreDiffs || !DoesFileExistedSomewhereInGeneratedCodeFolder(cSharpFileName)) // Don't generate a file if we are ignoring file creation while checking for edited content, unless the file doesn't exist at all.
                || !filesWithEdits.FindAll(x => x.cSharpScriptFileName == cSharpFileName).Any();

            if (generateScript && !isSimpleTest)
            {
                File.WriteAllText(saveTestFilePath, useSimplifiedDriverCode ? fullScriptWithSimplifiedDriverCode.ToString() : fullScriptWithoutSimplifiedDriverCode.ToString());
                CreateOrModifyPageObjectFiles(pageObjectProperties);
            }
            else if (generateScript)
            {
                GenerateAutomatedRunTest(automatedRunFileName, saveTestFilePath);
            }

            CultureInfo.DefaultThreadCurrentCulture = currentCulture;
            return filesWithEdits;
        }

        public static List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)> GenerateTestFromRecording(string recordingFileName, bool ignoreDiffs, bool isSimpleTest, bool useSimplifiedDriverCode, string stepFileToOverwrite = "")
        {
            currentCulture = CultureInfo.DefaultThreadCurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");

            // Create required files for UnityTest script creation.
            CreateTestAssemblyFolder();
            CreateTestAssembly();

            bool isStepFileOverwrite = !string.IsNullOrEmpty(stepFileToOverwrite);
            if (isStepFileOverwrite && !stepFileToOverwrite.EndsWith(".json"))
                stepFileToOverwrite += ".json";
            if (!recordingFileName.EndsWith(".json"))
                recordingFileName += ".json";
            string recordingFilePath = Path.Combine(AutomatedQASettings.RecordingDataPath, isStepFileOverwrite ? stepFileToOverwrite : recordingFileName);
            string className = $"Tests_{GetClassNameForRecording(recordingFilePath)}";

            // If this is a "simple" generated test, simply check if edits have been made to the file and return or overwrite accordingly.
            string cSharpFileName = $"{className}.cs";
            // For full tests, continue.
            List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)> filesWithEdits = new List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)>();

            // Get recording json.
            var json = string.Empty;
            if (File.Exists(recordingFilePath))
            {
                json = File.ReadAllText(recordingFilePath);
            }
            else if (File.Exists(recordingFilePath.Replace("_", "-")))
            {
                json = File.ReadAllText(recordingFilePath.Replace("_", "-"));
            }
            else if (File.Exists(recordingFilePath.Replace("-", "_")))
            {
                json = File.ReadAllText(recordingFilePath.Replace("-", "_"));
            }
            else
            {
                throw new Exception("Could not find the requested recording. Make sure the recording still exists after refreshing the list view. Check to make sure that the file name was not manually changed to include both underscores and dashes. Files with both characters in the same file name will not be found.");
            }

            string relativeRecordingFilePath = recordingFilePath.Split(new string[] { "Assets" }, StringSplitOptions.None).Last();
            List<(string file, TouchData data)> steps = new List<(string file, TouchData data)>();
            InputModuleRecordingData testData = JsonUtility.FromJson<InputModuleRecordingData>(json);
            TouchData playbackComplete = testData.touchData.Last();

            // Add main recording file's TouchData actions (if any).
            foreach (TouchData data in testData.touchData)
            {
                if (!testData.recordings.Any() && data.emitSignal != "playbackComplete")
                    steps.Add((recordingFilePath, data));
            }

            // If this recording has child segments, add their TouchData actions to the list of steps.
            if (!isStepFileOverwrite && testData.recordings.Any())
            {
                NestedSegmentHierarchy = (recordingFilePath, new List<string>());
                steps.AddRange(AddTouchDataFromSegments(testData.recordings, recordingFilePath));
                steps.Add((recordingFilePath, playbackComplete));
            }

            string stepFileClassName, stepFileCSharpName, lastTouchDataFile, lastJsonFileName;
            stepFileClassName = stepFileCSharpName = lastTouchDataFile = lastJsonFileName = string.Empty;
            List<string> touchIds = new List<string>();
            List<string> handledFiles = new List<string>();
            List<(string Scene, string PropertyName, string QuerySelector, string ObjectHierarchyPath)> pageObjectProperties = new List<(string, string, string, string)>();
            StringBuilder setUpLogic = new StringBuilder();
            StringBuilder stepFile = new StringBuilder();
            StringBuilder touchDataListForTestWithSimplifiedDriverLogic = new StringBuilder();
            StringBuilder touchDataListForTestWithoutSimplifiedDriverLogic = new StringBuilder();
            StringBuilder simpleScript = new StringBuilder(GetRecordedTestScript(recordingFileName));

            int index = 0;
            foreach ((string file, TouchData data) touchData in steps)
            {
                TouchData temptest = touchData.data;

                // If this is not the primary recording, but a step indicates the end of a recording or test, ignore it.
                if (touchData.data.emitSignal == "playbackComplete" || touchData.data.emitSignal == "segmentComplete")
                    continue;

                stepFileClassName = $"Steps_{GetClassNameForRecording(Path.Combine(AutomatedQASettings.RecordingDataPath, touchData.file))}";

                // Create a step file for each recording file.
                if (!isSimpleTest && !handledFiles.Contains(stepFileClassName))
                {
                    // Finish previous file.
                    if (handledFiles.Any())
                    {
                        stepFile.AppendLine($"{indentTwo}}}");
                        stepFile.AppendLine($"{indentOne}}}");
                        stepFile.AppendLine($"}}");
                        if (!isSimpleTest)
                            CreateStepFile(lastTouchDataFile, stepFileCSharpName, stepFile, ignoreDiffs, stepFileToOverwrite, ref filesWithEdits);
                    }

                    // Format recording name to remove characters that are invalid for use in a C# class name.
                    stepFileCSharpName = $"{stepFileClassName}.cs";
                    lastTouchDataFile = new FileInfo(touchData.file).Name;
                    lastTouchDataFile += lastTouchDataFile.EndsWith(".json") ? string.Empty : ".json";

                    handledFiles.Add(stepFileClassName);
                    stepFile = new StringBuilder();
                    touchIds = new List<string>();

                    // Start new step file. Steps will be references in the test file.
                    stepFile.AppendLine("using System.Collections.Generic;");
                    stepFile.AppendLine("using UnityEngine;");
                    stepFile.AppendLine($"using static UnityEngine.EventSystems.RecordingInputModule;{NEW_LINE}");
                    stepFile.AppendLine("namespace GeneratedAutomationTests");
                    stepFile.AppendLine("{"); // Start namespace bracket.
                    stepFile.AppendLine($"{indentOne}/// <summary>{NEW_LINE}" +
                                        $"{indentOne}/// This segment touch data were generated by Unity Automated QA for the recording from the Assets folder at \"{Path.Combine(AutomatedQASettings.RecordingFolderName, lastTouchDataFile)}\"{NEW_LINE}" +
                                        $"{indentOne}/// You can regenerate this file from the Unity Editor Menu: Automated QA > Generate Recorded Tests{NEW_LINE}" +
                                        $"{indentOne}/// </summary>");
                    stepFile.AppendLine($"{indentOne}public static class {stepFileClassName}");
                    stepFile.AppendLine($"{indentOne}{{");
                    stepFile.AppendLine($"{indentTwo}public static Dictionary<string, TouchData> Actions = new Dictionary<string, TouchData>();");
                    stepFile.AppendLine($"{indentTwo}static {stepFileClassName}()");
                    stepFile.AppendLine($"{indentTwo}{{");
                }

                // Handle generation of a C# TouchData object, which is a 1-to-1 correlation with TouchData stored in the json recording file.
                string waitSignal = string.IsNullOrEmpty(touchData.data.waitSignal) ? string.Empty : touchData.data.waitSignal;
                string emitSignal = string.IsNullOrEmpty(touchData.data.emitSignal) ? string.Empty : touchData.data.emitSignal;
                string objectName = string.IsNullOrEmpty(touchData.data.objectName) ? string.Empty : touchData.data.objectName;
                string objectTag = string.IsNullOrEmpty(touchData.data.objectTag) ? string.Empty : touchData.data.objectTag;
                string objectHierarchy = string.IsNullOrEmpty(touchData.data.objectHierarchy) ? string.Empty : touchData.data.objectHierarchy;
                string idName = (string.IsNullOrEmpty(touchData.data.objectName) ? ( // Use the object name as the first choice in generating an id for the touch data.
                                    string.IsNullOrEmpty(touchData.data.objectTag) ? ( // Use the object tag as the second choice in generating an id for the touch data.
                                        string.IsNullOrEmpty(waitSignal) ? ( // Use the wait signal as the third choice in generating an id for the touch data.
                                            string.IsNullOrEmpty(emitSignal) ? // Use the emit signal as the fourth choice in generating an id for the touch data.
                                                "Signal" : // If all other fields are empty (which they shouldn't be), then use this generic word in generating an id for the touch data.
                                                emitSignal
                                        ) :
                                        waitSignal
                                    ) :
                                    touchData.data.objectTag
                                ) :
                                touchData.data.objectName).Replace(" ", "_");
                string touchIdBase = $"{(touchData.data.eventType == TouchData.type.none ? "EMIT" : touchData.data.eventType.ToString().ToUpper())}_{idName}";
                string touchIdFinal = touchIdBase;
                int idIndex = 2;
                bool uniqueId = false;
                while (!uniqueId)
                {
                    if (touchIds.Contains(touchIdFinal))
                    {
                        touchIdFinal = $"{touchIdBase}_{idIndex}";
                        idIndex++;
                    }
                    else
                    {
                        touchIds.Add(touchIdFinal);
                        uniqueId = true;
                    }
                }

                stepFile.AppendLine($"{indentThree}Actions.Add(\"{touchIdFinal}\", new TouchData{NEW_LINE}" +
                                         $"{indentThree}{{{NEW_LINE}" +
                                         $"{indentFour}pointerId = {touchData.data.pointerId},{NEW_LINE}" +
                                         $"{indentFour}eventType = TouchData.type.{touchData.data.eventType},{NEW_LINE}" +
                                         $"{indentFour}timeDelta = {touchData.data.timeDelta}f,{NEW_LINE}" +
                                         $"{indentFour}position =  new Vector3({touchData.data.position.x}f, {touchData.data.position.y}f),{NEW_LINE}" +
                                         $"{indentFour}positional = {touchData.data.positional.ToString().ToLower()},{NEW_LINE}" +
                                         $"{indentFour}scene = \"{(string.IsNullOrEmpty(touchData.data.scene) ? testData.entryScene : touchData.data.scene)}\",{NEW_LINE}" +
                                         $"{indentFour}waitSignal = \"{waitSignal}\",{NEW_LINE}" +
                                         $"{indentFour}emitSignal = \"{emitSignal}\",{NEW_LINE}" +
                                         $"{indentFour}keyCode = \"{touchData.data.keyCode}\",{NEW_LINE}" +
                                         $"{indentFour}inputDuration = {touchData.data.inputDuration}f,{NEW_LINE}" +
                                         $"{indentFour}inputText = \"{touchData.data.inputText}\",{NEW_LINE}" +
                                         $"{indentFour}querySelector = \"{touchData.data.querySelector}\",{NEW_LINE}" +
                                         $"{indentFour}objectName = \"{objectName}\",{NEW_LINE}" +
                                         $"{indentFour}objectTag = \"{objectTag}\",{NEW_LINE}" +
                                         $"{indentFour}objectHierarchy = \"{objectHierarchy}\",{NEW_LINE}" +
                                         $"{indentFour}objectOffset =  new Vector3({touchData.data.objectOffset.x}f, {touchData.data.objectOffset.y}f){NEW_LINE}" +
                                         $"{indentThree}}});");

                string testStep = string.Empty;
                if (touchData.data.eventType != TouchData.type.release)
                {

                    /*
                     * Code will register each step file step used by a test in that test's SetUpClass method.
                     * This is because recording logic expects all TouchData to be set from the start of playback, as opposed to adding TouchData with each action we invoke.
                    */
                    string propertyName = string.Empty;
                    string methodNameOfAction = GetDriverMethodNameBasedOnActionType(touchData.data.eventType);
                    string queryString = DetermineBestQueryStringToFindGameObjectWith(touchData.data);

                    string elementType = string.Empty;
                    propertyName = queryString.ToCharArray()[0] == '#' ? queryString.Substring(1, queryString.Length - 1) : $"{(string.IsNullOrEmpty(touchData.data.scene) ? testData.entryScene : touchData.data.scene)}_{touchData.data.objectName}";
                    switch (touchData.data.eventType)
                    {
                        case TouchData.type.drag:
                            elementType = "Draggable";
                            break;
                        case TouchData.type.input:
                            elementType = "Input";
                            break;
                        case TouchData.type.press:
                            elementType = "Clickable";
                            break;
                        default:
                            elementType = "Element";
                            break;
                    }
                    propertyName = $"{elementType}_{propertyName.SanitizeStringForUseInGeneratingCode()}";
                    string pageObjectClassName = $"Scene_{(string.IsNullOrEmpty(touchData.data.scene) ? testData.entryScene : touchData.data.scene)}_PageObject";

                    // If this is a drag event, we need to provide the drag object and the target.
                    switch (touchData.data.eventType)
                    {
                        case TouchData.type.drag:
                            int indexRelease = steps.FindIndex(x => x == touchData) + 1;
                            if (indexRelease >= steps.Count)
                                throw new UnityException("A drag event was found in the recording that did not have a corresponding release events.");

                            string target = string.Empty;
                            // If target is not a GameObject, use a coordinate position.
                            if (string.IsNullOrEmpty(steps[indexRelease].data.objectName))
                            {
                                target = $"new Vector2({Math.Round(steps[indexRelease].data.position.x, 1)}f, {Math.Round(steps[indexRelease].data.position.y, 1)}f)";
                            }
                            else
                            {
                                target = $"\"{DetermineBestQueryStringToFindGameObjectWith(steps[indexRelease].data)}\"";

                            }
                            testStep = $"{indentThree}yield return Driver.Perform.Drag({pageObjectClassName}.{propertyName}, {target}, {Math.Round(steps[indexRelease].data.timeDelta, 1)}f);";
                            break;
                        case TouchData.type.input:
                            testStep = $"{indentThree}yield return Driver.Perform.SendKeys({pageObjectClassName}.{propertyName}, \"{touchData.data.inputText}\", {Math.Round(touchData.data.inputDuration, 1)}f);";
                            break;
                        case TouchData.type.press:
                            if (string.IsNullOrEmpty(touchData.data.querySelector) && string.IsNullOrEmpty(touchData.data.objectName))
                            {
                                testStep = $"{indentThree}yield return Driver.Perform.Click(new Vector2({Math.Round(touchData.data.position.x, 1)}f, {Math.Round(touchData.data.position.y, 1)}f));";
                            }
                            else
                            {
                                testStep = $"{indentThree}yield return Driver.Perform.Click({pageObjectClassName}.{propertyName});";
                            }
                            break;
                        case TouchData.type.button:
                        case TouchData.type.keyName:
                            testStep = $"{indentThree}yield return Driver.Perform.{methodNameOfAction}(\"{touchData.data.keyCode}\", {touchData.data.inputDuration}f);";
                            break;
                        case TouchData.type.key:
                            testStep = $"{indentThree}yield return Driver.Perform.{methodNameOfAction}(KeyCode.{touchData.data.keyCode}, {touchData.data.inputDuration}f);";
                            break;
                        default:
                            testStep = $"{indentThree}yield return Driver.Perform.{methodNameOfAction}({pageObjectClassName}.{propertyName});";
                            break;
                    }
                    touchDataListForTestWithSimplifiedDriverLogic.AppendLine(testStep);

                    // Handle creation of PageObject(s) so that references to query strings are used in tests, rather than the query selectors themselves (makes it easier to maintain and edit automation code).
                    if (!string.IsNullOrEmpty(queryString) && useSimplifiedDriverCode && (
                        touchData.data.eventType == TouchData.type.input ||
                        touchData.data.eventType == TouchData.type.drag ||
                        touchData.data.eventType == TouchData.type.press))
                    {
                        pageObjectProperties.Add((string.IsNullOrEmpty(touchData.data.scene) ? testData.entryScene : touchData.data.scene, propertyName, queryString, touchData.data.GetObjectScenePath()));
                    }
                }

                // For each step, add an extra line before the next TouchData object in our generated code.
                if (index != testData.touchData.Count - 1)
                    stepFile.AppendLine(string.Empty);

                setUpLogic.AppendLine($"{indentThree}Driver.Perform.RegisterStep({stepFileClassName}.Actions[\"{touchIdFinal}\"]);");
                testStep = $"{indentThree}yield return Driver.Perform.Action({stepFileClassName}.Actions[\"{touchIdFinal}\"]); " + (touchData.data.eventType == TouchData.type.none ? $"// Emit {touchData.data.emitSignal}" :
                            $"// Do a \"{touchData.data.eventType}\" action " +
                            (string.IsNullOrEmpty(touchData.data.objectName) ? $"at \"{Math.Round(touchData.data.position.x, 2)}x {Math.Round(touchData.data.position.y, 2)}y\" coordinates " : $"on \"{(string.IsNullOrEmpty(touchData.data.objectHierarchy) ? touchData.data.objectName : $"{touchData.data.objectHierarchy}/{touchData.data.objectName}")}\"") +
                            $"in scene \"{(string.IsNullOrEmpty(touchData.data.scene) ? testData.entryScene : touchData.data.scene)}\".");

                touchDataListForTestWithoutSimplifiedDriverLogic.AppendLine(testStep);
                index++;
            }

            // Finish final step file.
            stepFile.AppendLine($"{indentTwo}}}");
            stepFile.AppendLine($"{indentOne}}}");
            stepFile.AppendLine($"}}");
            if (!isSimpleTest && !useSimplifiedDriverCode)
                CreateStepFile(lastTouchDataFile, stepFileCSharpName, stepFile, ignoreDiffs, stepFileToOverwrite, ref filesWithEdits);

            /*
                Generate test file with step-by-step action invocations generated previously.
                Generate two different versions for the sake of comparing all possible unedited version of a test to guarantee customer edits are not overwritten.
            */
            StringBuilder universalCode = new StringBuilder();
            StringBuilder fullScriptWithSimplifiedDriverCode = new StringBuilder();
            StringBuilder fullScriptWithoutSimplifiedDriverCode = new StringBuilder();
            universalCode.AppendLine($"using System.Collections;{NEW_LINE}" +
                                  $"using UnityEngine;{NEW_LINE}" +
                                  $"using UnityEngine.TestTools;{NEW_LINE}" +
                                  $"using Unity.AutomatedQA;{NEW_LINE}" +
                                  $"using Unity.CloudTesting;{NEW_LINE}" +
                                  $"{NEW_LINE}namespace GeneratedAutomationTests{NEW_LINE}" +
                                  $"{{{NEW_LINE}" + // Start namespace bracket.
                                  $"{indentOne}/// <summary>{NEW_LINE}" +
                                  $"{indentOne}/// These tests were generated by Unity Automated QA for the recording from the Assets folder at \"{relativeRecordingFilePath}\".{NEW_LINE}" +
                                  $"{indentOne}/// You can regenerate this file from the Unity Editor Menu: Automated QA > Generate Recorded Tests{NEW_LINE}" +
                                  $"{indentOne}/// </summary>{NEW_LINE}" +
                                  $"{indentOne}public class {className} : AutomatedQATestsBase{NEW_LINE}" +
                                  $"{indentOne}{{{NEW_LINE}" + // Start class bracket.
                                  $"{indentTwo}/// Generated from recording file: \"{relativeRecordingFilePath}\".{NEW_LINE}" +
                                  $"{indentTwo}[UnityTest]{NEW_LINE}" +
                                  $"{indentTwo}[CloudTest]{NEW_LINE}" +
                                  $"{indentTwo}public IEnumerator CanPlayToEnd(){NEW_LINE}" +
                                  $"{indentTwo}{{");
            fullScriptWithSimplifiedDriverCode.Append(universalCode.ToString());
            fullScriptWithSimplifiedDriverCode.Append(touchDataListForTestWithSimplifiedDriverLogic.ToString());
            fullScriptWithoutSimplifiedDriverCode.Append(universalCode.ToString());
            fullScriptWithoutSimplifiedDriverCode.Append(touchDataListForTestWithoutSimplifiedDriverLogic.ToString());
            universalCode = new StringBuilder();
            string entryScene = !isSimpleTest && pageObjectProperties.Any() ? $"Scene_{pageObjectProperties.First().Scene}_PageObject.SceneName" : $"\"{testData.entryScene}\"";
            universalCode.AppendLine($"{indentTwo}}}{NEW_LINE}" +
                                  $"{indentTwo}// Initialize test run data.{NEW_LINE}" +
                                  $"{indentTwo}protected override void SetUpTestRun(){NEW_LINE}" +
                                  $"{indentTwo}{{{NEW_LINE}" +
                                  $"{indentThree}Test.entryScene = {entryScene};{NEW_LINE}" +
                                  $"{indentThree}Test.recordedAspectRatio = new Vector2({testData.recordedAspectRatio.x}f,{testData.recordedAspectRatio.y}f);{NEW_LINE}" +
                                  $"{indentThree}Test.recordedResolution = new Vector2({testData.recordedResolution.x}f,{testData.recordedResolution.y}f);{NEW_LINE}" +
                                  $"{indentTwo}}}{NEW_LINE}");
            fullScriptWithSimplifiedDriverCode.Append(universalCode.ToString());
            fullScriptWithoutSimplifiedDriverCode.Append(universalCode.ToString());
            universalCode = new StringBuilder();
            fullScriptWithoutSimplifiedDriverCode.AppendLine($"{indentTwo}// Steps defined by recording.{NEW_LINE}" +
                                    $"{indentTwo}protected override void SetUpTestClass(){NEW_LINE}" +
                                    $"{indentTwo}{{{NEW_LINE}" +
                                    $"{setUpLogic}" +
                                    $"{indentTwo}}}{NEW_LINE}");
            universalCode.AppendLine($"{indentOne}}}"); // End class bracket.
            universalCode.AppendLine($"}}"); // End namespace bracket.
            fullScriptWithSimplifiedDriverCode.AppendLine(universalCode.ToString());
            fullScriptWithoutSimplifiedDriverCode.AppendLine(universalCode.ToString());

            string saveTestFilePath = string.Empty;
            bool fileExists = DoesFileExistedSomewhereInGeneratedCodeFolder(cSharpFileName);
            if (fileExists)
                saveTestFilePath = FindFileSomewhereInGeneratedCodeFolder(cSharpFileName).FileLocation;
            else
                saveTestFilePath = Path.Combine(GeneratedTestsDirectory, cSharpFileName);
            // Compare new generated content to old generated content. If there is a difference, the user has made edits and we want to confirm that the user wishes to overwrite them.
            if (!isStepFileOverwrite && !ignoreDiffs && DoesFileExistedSomewhereInGeneratedCodeFolder(cSharpFileName))
            {
                if (HasGeneratedTestContentBeenEdited(cSharpFileName, fullScriptWithSimplifiedDriverCode.ToString(), fullScriptWithoutSimplifiedDriverCode.ToString(), simpleScript.ToString()))
                {
                    filesWithEdits.Add((recordingFileName, cSharpFileName, false));
                }
            }

            bool generateScript =
                !isStepFileOverwrite // Ignore the test file if we are only overwriting step files.
                && !isSimpleTest // Don't generate a full test script if it wasn't requested.
                && (ignoreDiffs || !DoesFileExistedSomewhereInGeneratedCodeFolder(cSharpFileName)) // Don't generate a file if we are ignoring file creation while checking for edited content, unless the file doesn't exist at all.
                || !filesWithEdits.FindAll(x => x.cSharpScriptFileName == cSharpFileName).Any();

            if (generateScript && !isSimpleTest)
            {
                File.WriteAllText(saveTestFilePath, useSimplifiedDriverCode ? fullScriptWithSimplifiedDriverCode.ToString() : fullScriptWithoutSimplifiedDriverCode.ToString());
                CreateOrModifyPageObjectFiles(pageObjectProperties);
            }
            else if (generateScript)
            {
                GenerateSimpleTest(recordingFilePath, saveTestFilePath);
            }

            CultureInfo.DefaultThreadCurrentCulture = currentCulture;
            return filesWithEdits;
        }

        /// <summary>
        /// Recursive tool for adding steps from segements and nested sub-segments referenced by all associated json files in a recording.
        /// Record files used previously in the hierarchy of segment relationships, and throw an error if there is a circular reference.
        /// </summary>
        /// <param name="recordings"></param>
        private static List<(string file, TouchData data)> AddTouchDataFromSegments(List<Recording> recordings, string parentFile)
        {
            // This is how we will determine that a hierarchical reference of segments from the top level down to the last child do not invoke a circular reference to a segment higher up in the tree of segments.
            if (NestedSegmentHierarchy.segments.Any() && parentFile != NestedSegmentHierarchy.segments.Last())
            {
                NestedSegmentHierarchy = (parentFile, new List<string>()); // This is a top level recording, with no parent segments. Start mapping out new hierarchy of segments that it references.
            }

            List<(string file, TouchData data)> steps = new List<(string file, TouchData data)>();
            foreach (Recording recording in recordings)
            {
                // This segment was already reference higher up in the stack, thus creating a circular relationship. Any resulting composite recoridng would be invalid and effectively infinite.
                if (NestedSegmentHierarchy.segments.Contains(recording.filename))
                {
                    throw new UnityException("Circular reference encountered. The chosen recording has references to segments that reference a parent segment. " +
                        "This creates a circular reference, which is an invalid use of modular segments and recordings. The path from top recording to circular reference is: " +
                        $"{parentFile} > {string.Join(" > ", NestedSegmentHierarchy.segments)}");
                }
                NestedSegmentHierarchy.segments.Add(recording.filename);

                // Get this segment's data, and see if it too references child recordings.
                var jsonSegment = File.ReadAllText(Path.Combine(Application.dataPath, AutomatedQASettings.RecordingFolderName, recording.filename));
                InputModuleRecordingData segmentData = JsonUtility.FromJson<InputModuleRecordingData>(jsonSegment);
                if (segmentData.recordings.Any())
                {
                    steps.AddRange(AddTouchDataFromSegments(segmentData.recordings, recording.filename));
                }

                // Ignore segment complete emits. Generated code does not behave like a composite recording.
                foreach (TouchData data in segmentData.touchData)
                {
                    if (data.emitSignal != "segmentComplete")
                        steps.Add((recording.filename, data));
                }
            }
            return steps;
        }

        /// <summary>
        /// Add spaces to a string based on requested indentiation tab count.
        /// </summary>
        /// <param name="tabCount"></param>
        /// <returns></returns>
        public static string GetIndentationString(int tabCount)
        {
            StringBuilder tabs = new StringBuilder();
            for (int x = 0; x < tabCount; x++)
            {
                tabs.Append("    ");
            }
            return tabs.ToString();
        }

        /// <summary>
        /// Strip all spacing, new line, and carriage return characters which may change after file generation, and should not be considered a customization of our generated content.
        /// </summary>
        private static bool HasGeneratedTestContentBeenEdited(string filePath, string fullScriptWithSimplifiedDriverLogic, string fullScriptWithoutSimplifiedDriverLogic, string simpleScript)
        {
            // If neither a full generated test, nor a simple generated test are identical to the newly-generated file, then the file content has been edited.
            return IsFullFileEdited(filePath, fullScriptWithSimplifiedDriverLogic) && IsFullFileEdited(filePath, fullScriptWithoutSimplifiedDriverLogic) && isSimpleFileEdited(filePath, simpleScript);
        }

        /// <summary>
        /// Does a new file built as a full test script match the old file?
        /// </summary>
        private static bool IsFullFileEdited(string filePath, string fullScript)
        {
            if (!DoesFileExistedSomewhereInGeneratedCodeFolder(filePath)) return false;
            string existingFileContent = FindFileSomewhereInGeneratedCodeFolder(filePath).FileContents.Replace(" ", string.Empty).Replace(NEW_LINE, string.Empty);
            string newFullFileContent = fullScript.ToString().Replace(" ", string.Empty).Replace(NEW_LINE, string.Empty);
            return existingFileContent != newFullFileContent;
        }

        /// <summary>
        /// Does a new file built as a simple test script match the old file?
        /// </summary>
        private static bool isSimpleFileEdited(string filePath, string simpleScript)
        {
            if (!DoesFileExistedSomewhereInGeneratedCodeFolder(filePath)) return false;
            string existingFileContent = FindFileSomewhereInGeneratedCodeFolder(filePath).FileContents.Replace(" ", string.Empty).Replace(NEW_LINE, string.Empty);
            string newSimpleFileContent = simpleScript.ToString().Replace(" ", string.Empty).Replace(NEW_LINE, string.Empty);
            return existingFileContent != newSimpleFileContent;
        }

        /// <summary>
        /// Generate new file containing all of the TouchData from a recording json file.
        /// </summary>
        /// <param name="stepFileName">Name of file without path or extension.</param>
        /// <param name="stepFile">File content to save to file.</param>
        /// <param name="ignoreDiffs">Do not compare new file content to old content.</param>
        /// <param name="stepFilesToOverwrite">List of step files that the user has chosen to overwrite.</param>
        /// <param name="filesWithEdits">Reference to list of C# files where customized edits where detected.</param>
        private static void CreateStepFile(string stepFileRecording, string stepFileCSharpName, StringBuilder stepFile, bool ignoreDiffs, string stepFileToOverwrite, ref List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)> filesWithEdits)
        {
            string stepFilesDirectory = Path.Combine(AutomatedQASettings.PackageAssetsFolderPath, AutomatedQASettings.GeneratedTestsFolderName, "Steps");
            (string FileLocation, string FileContents) file = (string.Empty, string.Empty);
            bool doesFileExist = DoesFileExistedSomewhereInGeneratedCodeFolder(stepFileCSharpName);
            if (doesFileExist)
            {
                file = FindFileSomewhereInGeneratedCodeFolder(stepFileCSharpName);
            }
            else
            {
                file = (Path.Combine(stepFilesDirectory, stepFileCSharpName), stepFile.ToString());
                if (!Directory.Exists(stepFilesDirectory))
                {
                    Directory.CreateDirectory(stepFilesDirectory);
                }
            }

            // Check if this file has edits and report if so.
            if (!ignoreDiffs && IsFullFileEdited(stepFileCSharpName, stepFile.ToString()))
            {
                filesWithEdits.Add((stepFileRecording, stepFileCSharpName, true));
            }
            // If this is a call for file creation & overwrites, check that this file was requested to be overwritten and then generate it.
            else if (!doesFileExist || (ignoreDiffs && stepFileToOverwrite.Replace(".json", string.Empty) == stepFileRecording.Replace(".json", string.Empty)))
            {
                File.WriteAllText(file.FileLocation, stepFile.ToString());
            }
        }

        /// <summary>
        /// Create the folder where tests will be stored.
        /// </summary>
        public static void CreateTestAssemblyFolder()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, AutomatedQASettings.PackageAssetsFolderName, GeneratedTestsFolderName));
        }

        /// <summary>
        /// Create the assembly that allows UnityTests to be compiled.
        /// </summary>
        public static void CreateTestAssembly()
        {
            var template = File.ReadAllText(GeneratedTestAssemblyTemplatePath);
            var content = template.Replace("#SCRIPTNAME#", Path.GetFileNameWithoutExtension(GeneratedTestsAssemblyName));
            string path = Path.Combine(Application.dataPath, AutomatedQASettings.PackageAssetsFolderName, GeneratedTestsFolderName, GeneratedTestsAssemblyName);
            if (!DoesFileExistedSomewhereInGeneratedCodeFolder(path))
                File.WriteAllText(path, content);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="runPath"></param>
        private static void GenerateAutomatedRunTest(string runPath, string savePath)
        {
            CreateTestAssemblyFolder();
            CreateTestAssembly();
            CreateAutomatedRunTestScripts(runPath, savePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        private static void CreateAutomatedRunTestScripts(string runPath, string savePath)
        {
            string content = GetAutomatedRunTestScript(runPath);
            EditorUtility.DisplayProgressBar("Generate Automated Run Test", $"Create Automated Run Test Scripts: {savePath}", 0);
            File.WriteAllText(savePath, content);
        }

        /// <summary>
        /// Generate a simple test from a recording. This test has one line to invoke the playback of a recording, and an assertion to mark the test as passed on completion.
        /// This test has minimal capability to be modified and customized by the user.
        /// </summary>
        /// <param name="recording"></param>
        private static void GenerateSimpleTest(string recording, string savePath)
        {
            var recordingFilePath = recording.Split(new string[] { "Assets" }, StringSplitOptions.None).Last().Trim('/').Trim('\\');
            File.WriteAllText(savePath, GetRecordedTestScript(recordingFilePath));
        }

        /// <summary>
        /// Gets the template of a simple test.
        /// </summary>
        /// <param name="recording"></param>
        /// <returns></returns>
        private static string GetRecordedTestScript(string recording)
        {
            string templateContent = File.ReadAllText(GeneratedTestScriptTemplatePath);
            var testClassName = $"Tests_{GetClassNameForRecording(recording)}";
            return templateContent
                .Replace("#RECORDING_NAME#", testClassName)
                .Replace("#RECORDING_FILE#", AutomatedQASettings.RecordingFolderName + "/" + Path.GetFileName(recording));
        }

        /// <summary>
        /// Gets the template of a simple test.
        /// </summary>
        /// <param name="recording"></param>
        /// <returns></returns>
        private static string GetAutomatedRunTestScript(string automatedRun)
        {
            string runName = Path.GetFileNameWithoutExtension(automatedRun);
            string templateContent = File.ReadAllText(GeneratedAutomationTestscriptTemplatePath);
            var runFilePath = automatedRun;
            var testClassName = $"Tests_{runName}";
            return templateContent
                .Replace("#CLASS_NAME#", testClassName)
                .Replace("#RUN_FILEPATH#", runFilePath)
                .Replace("#RUN_NAME#", runName);
        }

        /// <summary>
        /// Generate a class name for a recording.
        /// </summary>
        /// <param name="recordingFilePath"></param>
        /// <returns></returns>
        private static string GetClassNameForRecording(string recordingFilePath)
        {
            var testClassName = Path.GetFileNameWithoutExtension(recordingFilePath);
            testClassName = testClassName.SanitizeStringForUseInGeneratingCode();
            return testClassName;
        }

        /// <summary>
        /// Provide the associated Driver.cs method that will perform the requested action.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private static string GetDriverMethodNameBasedOnActionType(TouchData.type action)
        {
            switch (action)
            {
                case TouchData.type.press:
                    return "Click";
                case TouchData.type.drag:
                    return "Drag";
                case TouchData.type.input:
                    return "SendKeys";
                case TouchData.type.keyName:
                case TouchData.type.key:
                    return "KeyDown";
                case TouchData.type.button:
                    return "PressControllerButton";
                case TouchData.type.release:
                    return "Action";
                default:
                    throw new UnityException($"The provided action type [{action}] does not have a defined value to associate it with a driver action.");
            }
        }

        /// <summary>
        /// Generated a query string that will be as flexible and uniquely-identifying as possible.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string DetermineBestQueryStringToFindGameObjectWith(TouchData data)
        {
            if (!string.IsNullOrEmpty(data.querySelector))
            {
                return data.querySelector;
            }
            else
            {
                return $"{data.objectHierarchy}/{data.objectName}";
            }
        }

        /// <summary>
        /// Generates files for Page Objects and the fields they contain.
        /// </summary>
        /// <param name="pageObjectProperties"></param>
        private static void CreateOrModifyPageObjectFiles(List<(string, string, string, string)> pageObjectProperties)
        {
            string pageObjectDirectory = Path.Combine(AutomatedQASettings.PackageAssetsFolderPath, AutomatedQASettings.GeneratedTestsFolderName, "PageObjects");
            // Create PageObject for properties.
            foreach ((string Scene, string PropertyName, string QuerySelector, string ObjectHierarchyPath) property in pageObjectProperties)
            {
                StringBuilder pageObject = new StringBuilder();
                string pageObjectClassName = $"Scene_{property.Scene}_PageObject";
                string pageObjectFileName = $"{pageObjectClassName}.cs";
                string pageObjectFilePath = string.Empty;
                bool pageObjectExists = DoesFileExistedSomewhereInGeneratedCodeFolder(pageObjectFileName);
                if (pageObjectExists)
                {
                    pageObjectFilePath = FindFileSomewhereInGeneratedCodeFolder(pageObjectFileName).FileLocation;
                }
                else
                {
                    if (!Directory.Exists(pageObjectDirectory))
                        Directory.CreateDirectory(pageObjectDirectory);
                    pageObjectFilePath = Path.Combine(pageObjectDirectory, pageObjectFileName);
                }

                string sceneNameField = $"{indentTwo}public static string SceneName = \"{property.Scene}\";";
                string rawExistingPageObjectCode = string.Empty;
                if (pageObjectExists)
                {
                    rawExistingPageObjectCode = string.Join(string.Empty, TrimClassAndNamespaceBracketsForAppendingNewPropertiesToPageObject(File.ReadAllText(pageObjectFilePath)).ToArray()); // Remove final brackets to append new properties.
                    pageObject.Append(rawExistingPageObjectCode);
                }
                else
                {
                    pageObject.AppendLine($"namespace GeneratedAutomationTests{NEW_LINE}" +
                                            $"{{{NEW_LINE}" +
                                            $"{indentOne}public class {pageObjectClassName}{NEW_LINE}" +
                                            $"{indentOne}{{{NEW_LINE}" +
                                            sceneNameField);
                }
                if (!rawExistingPageObjectCode.Contains(property.PropertyName))
                {
                    string comment = property.QuerySelector.StartsWith("@")
                        ? $"// Scene hierarchy path: [\"{property.ObjectHierarchyPath}\"]"
                        : "";
                    pageObject.AppendLine($"{NEW_LINE}{indentTwo}public static string {property.PropertyName} = \"{property.QuerySelector}\"; {comment}");

                }
                if (pageObjectExists && !rawExistingPageObjectCode.Contains("SceneName"))
                {
                    pageObject.AppendLine($"{NEW_LINE}{sceneNameField}");
                }
                pageObject.AppendLine($"{indentOne}}}");
                pageObject.AppendLine($"}}");
                File.WriteAllText(pageObjectFilePath, pageObject.ToString());
            }
        }

        /// <summary>
        /// Removes the final closing brackets from the class and namespace declarations to append more fields to the Page Object provided.
        /// </summary>
        /// <param name="cSharpCode"></param>
        /// <returns></returns>
        private static List<string> TrimClassAndNamespaceBracketsForAppendingNewPropertiesToPageObject(string cSharpCode) {
            List<string> returnVal = new List<string>();
            List<char> invertedFileContents = cSharpCode.ToCharArray().ToList();
            invertedFileContents.Reverse();
            int closingBracketsRemoved = 0;
            foreach (char character in invertedFileContents)
            {
                if (closingBracketsRemoved < 2)
                {
                    if (character == '}')
                    {
                        closingBracketsRemoved++;
                    }
                }
                else
                {
                    returnVal.Add(character.ToString());
                }
            }
            returnVal.Reverse();
            if (returnVal.Count == 0)
                throw new UnityException("Existing Page Object file was found, but parser could not read the file to append new fields.");
            return returnVal;
        }


        /// <summary>
        /// Determine if there is a pre-existing file.
        /// </summary>
        /// <param name="fileName"></param>
        private static bool DoesFileExistedSomewhereInGeneratedCodeFolder(string fileName)
        {
            List<string> files = Directory.GetFiles(GeneratedTestsDirectory, "*.cs", SearchOption.AllDirectories).ToList();
            return files.FindAll(x => x.Split(Path.DirectorySeparatorChar).Last() == fileName).Any();
        }

        /// <summary>
        /// Get contents of an existing Test, Page Object, or Steps file.
        /// </summary>
        /// <param name="fileName"></param>
        private static (string FileLocation, string FileContents) FindFileSomewhereInGeneratedCodeFolder(string fileName)
        {
            List<string> files = Directory.GetFiles(GeneratedTestsDirectory, "*.cs", SearchOption.AllDirectories).ToList();
            string result = files.Find(x => x.Split(Path.DirectorySeparatorChar).Last() == fileName);
            return (result, File.ReadAllText(result));
        }
    }
}