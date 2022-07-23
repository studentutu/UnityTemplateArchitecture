using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Unity.RecordedTesting
{
    [Serializable]
    public struct Metadata
    {
        public string deviceUUID;
        public string attemptId;
        public string gameSimSettings;

        public Metadata(string deviceUUID, string attemptId, string gameSimSettings)
        {
            this.deviceUUID = deviceUUID;
            this.attemptId = attemptId;
            this.gameSimSettings = gameSimSettings;
        }
    }
        
    [Serializable]
    struct CountersData
    {
        public Metadata metadata;
        public Counter[] items;

        public CountersData(Dictionary<string,Counter> counters, Metadata metadata)
        {
            items = new Counter[counters.Count];
            int index = 0;
            foreach (var kvp in counters)
                items[index++] = counters[kvp.Key];
            this.metadata = metadata;
        }
    }

    [Serializable]
    public struct SignedUrlResponse
    {
        public string response;
    }
    
    [Serializable]
    public struct DeviceFarmConfig
    {
        public string testName;
        public string packageName;
        public string settingsFileToLoad;
        public string unityDeviceTestingJobId;
        public string unityProjectId;
        public string unityOrgId;
        public string awsDeviceUDID;
        public string awsDeviceModel;
        public string awsDeviceName;
        public string awsDeviceOS;
    }

    public class DeviceFarmOverrides
    {
        public string testName;
        public string awsDeviceUDID;
        public string awsDeviceModel;

        public DeviceFarmOverrides(string testName, string deviceModel)
        {
            this.testName = testName;
            this.awsDeviceModel = deviceModel;
        }
    }

    [Serializable]
    internal class Counter
    {
        [SerializeField] 
        string _name;

        public string Name { get { return _name; } }

        [SerializeField] 
        internal Int64 _value;
        
        public Int64 Value { get { return _value; } }

        public Counter(string name)
        {
            _name = name;
            Reset();
        }

        internal Int64 Increment(Int64 amount)
        {
            return Interlocked.Add(ref _value, amount);
        }

        internal void Reset(Int64 value = 0)
        {
            Interlocked.Exchange(ref _value, value);
        }

    }
}