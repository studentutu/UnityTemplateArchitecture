using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace App.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoPlayerAudioSourceOutput : MonoBehaviour
    {
        [SerializeField] private VideoPlayer player;
        [SerializeField] private AudioSource audioSource;
        
        
        private void OnValidate()
        {
            if (player == null)
            {
                player = GetComponent<VideoPlayer>();
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public void SetTargetAudio(AudioSource audioSourceTarget)
        {
            if (player.audioTrackCount > 1)
            {
                Debug.LogWarning("VIdeo CLip has more than one audio track output!");
            }

            for (int i = 0; i < player.audioTrackCount; i++)
            {
                player.SetTargetAudioSource((ushort)i, audioSourceTarget);
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        public void SetTargetAudioForTrack(int track, AudioSource audioSourceTarget)
        {
            player.SetTargetAudioSource((ushort)track, audioSourceTarget);
        }

        private void OnEnable()
        {
            if (player != null && audioSource != null && player.audioOutputMode == VideoAudioOutputMode.AudioSource)
            {
                SetTargetAudio(audioSource);
            }
        }
    }
}