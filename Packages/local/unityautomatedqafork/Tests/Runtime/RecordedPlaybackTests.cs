using System.IO;
using NUnit.Framework;
using Unity.RecordedPlayback;

namespace Unity.AutomatedQA.Tests
{
    public class RecordedPlaybackTests
    {
        [Test]
        public void CreateNewRecordingWithoutFiles()
        {
            string recordingPath = RecordedPlaybackPersistentData.GetRecordingDataFilePath();
            string configPath = RecordedPlaybackPersistentData.GetConfigFilePath();

            if(File.Exists(recordingPath))
            {
                File.Delete(recordingPath);
            }
            
            if(File.Exists(configPath))
            {
                File.Delete(configPath);
            }

            RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Record);
            RecordedPlaybackController.Instance.Begin();

            Assert.That(RecordedPlaybackController.Exists());
        }

    }
}
