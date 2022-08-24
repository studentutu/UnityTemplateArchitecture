using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.AutomatedQA;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity.RecordedPlayback
{
    public class RecordingConfig
    {
        public RecordingMode mode;
        public string recordingFileName;
    }

    public enum RecordingMode
    {
        None,
        Record,
        Playback,
        Extend
    }

    public abstract class BaseRecordingData
    {
        public void SaveToFile(string filepath)
        {
            var destDir = Path.GetDirectoryName(filepath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.WriteAllText(filepath, JsonUtility.ToJson(this, true));
        }
    }

    public static class RecordedPlaybackPersistentData
    {
        public static string RecordingFileName { get; set; }
        public static Vector2 RecordedResolution { get; set; }
        public static Vector2 RecordedAspectRatio { get; set; }
        public const string kRecordedPlaybackFilename = "playback_recording.json";
        public const string kRecordedPlaybackConfigFilename = "config_recording.json";

        public static void SetRecordingMode(RecordingMode mode, string recordingFileName = "")
        {
            RecordingFileName = recordingFileName;
            // TODO: "RecordedPlaybackConfig.Mode = mode" instead of file io
            var output = new RecordingConfig { mode = mode, recordingFileName = RecordingFileName };
            var configJson = JsonUtility.ToJson(output);
            
            
            var destDir = Path.GetDirectoryName(GetConfigFilePath());
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            
            File.WriteAllText(GetConfigFilePath(), configJson);
        }

        public static RecordingMode GetRecordingMode()
        {
            try
            {
                var configJson = File.ReadAllText(GetConfigFilePath());
                var config = JsonUtility.FromJson<RecordingConfig>(configJson);
                RecordingFileName = config.recordingFileName;
                return config.mode;
            }
            catch (FileNotFoundException)
            {
                var recordingMode = RecordingMode.None;
                var output = new RecordingConfig { mode = recordingMode, recordingFileName = RecordingFileName };
                var configJson = JsonUtility.ToJson(output);
                
                var destDir = Path.GetDirectoryName(GetConfigFilePath());
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                
                File.WriteAllText(GetConfigFilePath(), configJson);
                return recordingMode;
            }

        }

        public static T GetRecordingData<T>() where T : BaseRecordingData
        {
            string path = GetRecordingDataFilePath();
            string text = "{}";
            if (File.Exists(path))
                text = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(text);
        }

        public static void SetRecordingData(BaseRecordingData recordingData)
        {
            recordingData.SaveToFile(GetRecordingDataFilePath());
        }
        
        public static void SetRecordingData(string rawData)
        {
            RecordedPlaybackPersistentData.CleanRecordingData();
            File.WriteAllText(GetRecordingDataFilePath(), rawData);
        }

        public static void SetRecordingDataFromFile(string sourcePath)
        {
            AQALogger logger = new AQALogger();

            RecordedPlaybackPersistentData.CleanRecordingData();
            string destPath = Path.Combine(AutomatedQASettings.PersistentDataPath, kRecordedPlaybackFilename);

            if (!string.IsNullOrEmpty(sourcePath))
            {
                CopyRecordingFile(sourcePath, destPath);
            }
            else
            {
                logger.LogError($"Failed to copy recording file from {sourcePath} to {destPath}");
            }
        }

        public static string GetRecordingDataFilePath()
        {
            return Path.Combine(AutomatedQASettings.PersistentDataPath, kRecordedPlaybackFilename);
        }

        public static string GetConfigFilePath()
        {
            return Path.Combine(AutomatedQASettings.PersistentDataPath, kRecordedPlaybackConfigFilename);
        }

        public static List<string> GetSegmentFiles(string fileName)
        {
            AQALogger logger = new AQALogger();

            try
            {
                var text = File.ReadAllText(fileName);
                var recordingFile = JsonUtility.FromJson<RecordingInputModule.InputModuleRecordingData>(text);

                return recordingFile.recordings.Select(x => x.filename);
            }
            catch (ArgumentException)
            {
                logger.LogWarning($"{fileName} is not a valid recording json file");
            }

            return new List<string>();
        }

        public static void CleanRecordingData()
        {
            Directory.CreateDirectory(Path.Combine(AutomatedQASettings.PersistentDataPath,
                AutomatedQASettings.PackageAssetsFolderName));
            
            string recordingFile =
                Path.Combine(AutomatedQASettings.PersistentDataPath, kRecordedPlaybackFilename);
            if(File.Exists(recordingFile))
            {
                File.Delete(recordingFile);
            }
            
            var regex = new Regex("recording_segment_.*\\.json");
            foreach (var segment in Directory.EnumerateFiles(AutomatedQASettings.PersistentDataPath).ToList().FindAll(x => regex.IsMatch(Path.GetFileName(x))))
            {
                File.Delete(segment);
            }
        }

        public static List<string> CopyRecordingFile(string sourcePath, string destPath)
        {
            AQALogger logger = new AQALogger();

            var createdFiles = new List<string>();
            var destDir = Path.GetDirectoryName(destPath) ?? "";
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            if (!File.Exists(sourcePath))
            {
                logger.LogException(new UnityException($"Required recording file does not exist [{sourcePath}]."));
            }
            File.Copy(sourcePath, destPath, true);
            createdFiles.Add(destPath);

            var recordingFiles = GetSegmentFiles(sourcePath);
            foreach (var recordingFile in recordingFiles)
            {
                var segmentPath = Path.Combine(Path.GetDirectoryName(sourcePath), recordingFile);
                var segmentDest = Path.Combine(destDir, recordingFile);
                File.Copy(segmentPath, segmentDest, true);
                createdFiles.Add(segmentDest);
            }
            logger.Log($"Copied recording file from {sourcePath} to {destPath}");

            return createdFiles;
        }
    }


}