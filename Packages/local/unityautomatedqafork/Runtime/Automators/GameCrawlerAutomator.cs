using System;
using System.Collections;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Listeners;
using UnityEngine;
using UnityEngine.EventSystems;

[Serializable]
public class GameCrawlerAutomatorConfig : AutomatorConfig<GameCrawlerAutomator>
{
    public float CrawlTimeout = 300f;
    public float WaitForNextStepTimeout = 25f;
    public float MaxTimeStuckBeforeFailing = 45f;
    public float WaitTimeBetweenAttemptingNextAction = 3f;
    // Set to 1800 seconds (half an hour). The amount of data in runs over a half an hour may be large and require storage considerations.
    public float SecondsToRunBeforeSkippingGenerationOfAReport = 1800f;   
    public bool RunUntilStuck;
}

public class GameCrawlerAutomator : Automator<GameCrawlerAutomatorConfig>
{
    public override void BeginAutomation()
    {
        base.BeginAutomation();
        gameObject.AddComponent<GameListenerHandler>();
        GameCrawler gc = gameObject.AddComponent<GameCrawler>();
        gc.Initialize(config);
        StartCoroutine(WaitForCrawler());
    }

    private IEnumerator WaitForCrawler()
    {
        while (GameCrawler.IsCrawling && !GameCrawler.IsStuck && !GameCrawler.Stop)
        {
            yield return null;
        }

        if (RecordingInputModule.Instance != null)
        {
            var recordingData = new RecordingInputModule.InputModuleRecordingData();
            recordingData.touchData = RecordingInputModule.Instance.GetTouchData();
            recordingData.AddPlaybackCompleteEvent();
            recordingData.recordedAspectRatio = new Vector2(Screen.width, Screen.height);
            recordingData.recordedResolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
            ReportingManager.AddRecordingData(recordingData);
        }

        EndAutomation();
    }
}