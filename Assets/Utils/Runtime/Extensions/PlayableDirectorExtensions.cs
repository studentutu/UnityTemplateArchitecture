using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

public static class PlayableDirectorExtensions
{
    public static bool IsPlaying(this PlayableDirector self)
    {
        if (!self.enabled || self.state == PlayState.Paused)
        {
            return false;
        }

        if (self.GetCurrentSpeed() == 0)
        {
            return false;
        }

        if (Mathf.Abs((float) (self.duration - self.time)) < 0.001)
        {
            return false;
        }

        return true;
    }

    public static void StopAndEvaluate(this PlayableDirector self, bool rewind)
    {
        self.Stop();
        bool enabledPrevious = self.enabled;
        self.enabled = true;
        if (rewind)
        {
            self.time = 0;
        }
        else
        {
            if (!self.playableGraph.IsValid())
            {
                self.enabled = enabledPrevious;
                return;
            }

            self.time = self.duration;
        }

        self.Evaluate();
        self.enabled = enabledPrevious;
    }

    public static void Replay(this PlayableDirector self)
    {
        self.Stop();
        self.enabled = true;
        self.time = 0;
        self.ResumeDirector(1);
        self.enabled = true;
        self.Play();
    }

    public static UniTask ReplayAsync(this PlayableDirector self)
    {
        self.Stop();
        self.enabled = true;
        self.time = 0;
        self.ResumeDirector(1);
        return self.PlayAsync();
    }

    public static UniTask PlayAsync(this PlayableDirector self)
    {
        self.enabled = true;
        self.Play();
        if (!self.playableGraph.IsValid())
        {
            return UniTask.CompletedTask;
        }

        return UniTask.WaitWhile(() => { return self != null && self.IsPlaying(); });
    }

    /// <summary>
    /// Resumes the TimelineInstance referenced in the playable director.
    /// </summary>
    /// <param name="director">The director to be resumed</param>
    /// <param name="speed">Playback speed for the director</param>
    public static void ResumeDirector(this PlayableDirector director, float speed)
    {
        if (director.playableAsset != null && director.playableGraph.IsValid())
        {
            director.enabled = true;
            director.playableGraph.GetRootPlayable(0).SetSpeed(speed);
        }
    }

    /// <summary>
    /// Pauses the TimelineInstance referenced in the playable director.
    /// </summary>
    /// <param name="director">The director to be resumed</param>
    /// <returns>Current playback speed for the director</returns>
    public static float PauseDirector(this PlayableDirector director)
    {
        float currentSpeed = director.GetCurrentSpeed();
        director.playableGraph.GetRootPlayable(0).SetSpeed(0);
        return currentSpeed;
    }

    public static float GetCurrentSpeed(this PlayableDirector director)
    {
        if (director == null || director.playableAsset == null || !director.playableGraph.IsValid())
        {
            return 1;
        }

        return (float) director.playableGraph.GetRootPlayable(0).GetSpeed();
    }
}