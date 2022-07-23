using System;

namespace Unity.RecordedTesting
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RecordedTestAttribute : Attribute
    {
        string recording;

        public RecordedTestAttribute(string recording)
        {
            this.recording = recording;
        }

        public string GetRecording()
        {
            return recording;
        }
    }

}