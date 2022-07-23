using System;
using System.Collections;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using UnityEngine;

public class ReportingMonitor : MonoBehaviour
{
    public static ReportingMonitor Instance { get; set; }
    public RecordingMode RecordingMode
    {
        get
        {
            return _recordingMode;
        }
        set
        {
            _recordingMode = value;
        }
    }
    private RecordingMode _recordingMode;

    public static float HeapSampleRate { get; set; }

    void Start()
    {
        if (Mathf.Approximately(HeapSampleRate, 0f))
            HeapSampleRate = 1f;
        Instance = this;
        StartCoroutine(TrackPerformance());
    }

    float fpsPool = 0;
    int fpsSamples = 0;
    private bool trackingPerformance = false;
    private IEnumerator TrackPerformance()
    {
        AQALogger logger = new AQALogger();
        if (trackingPerformance)
        {
            yield return null;
        }
        else
        {
            trackingPerformance = true;
            while (Application.isPlaying)
            {
                for (float x = 0; x <= 0.5f; x += Time.deltaTime)
                {
                    fpsSamples++;
                    fpsPool += (float)Math.Round(1.0f / Time.deltaTime, 0);
                    yield return null;
                }
                float framerate = fpsPool / fpsSamples;
                fpsPool = fpsSamples = 0;

                ReportingManager.SamplePerformance(avgFps: framerate);
            }
        }
    }

    /// <summary>
    /// For Windows Store & Android "end state".
    /// </summary>
    /// <param name="pause"></param>
    private void OnApplicationFocus(bool hasFocus)
    {
#if !UNITY_EDITOR
        if (!hasFocus&& RecordingMode == RecordingMode.Playback)
        {
            ReportingManager.FinalizeReport();
        }
#endif
    }

    /// <summary>
    /// For iOS "end state".
    /// </summary>
    /// <param name="pause"></param>
    private void OnApplicationPause(bool pause)
    {
#if !UNITY_EDITOR
        if (RecordingMode == RecordingMode.Playback)
        {
            ReportingManager.FinalizeReport();
        }
#endif
    }

    /// <summary>
    /// For editor, standalone, and other platform's.
    /// </summary>
    private void OnApplicationQuit()
    {
        if (!ReportingManager.IsPlaybackStartedFromEditorWindow && (RecordingMode == RecordingMode.Playback || ReportingManager.IsCrawler))
        {
            ReportingManager.FinalizeReport();
        }
    }

    public static void Destroy(ReportingMonitor instance)
    {
        Destroy(instance);
    }
}