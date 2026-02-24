using System;
using UnityEngine;
using System.Collections.Generic;

namespace Piximate
{
    public class Piximator : MonoBehaviour
    {
        private const float PlaybackThreshold = 0.01F;

        public event Action<string> AnimationFinished;
        public event Action<string> AnimationLooped;
        public event Action<int> FrameChanged;

        [SerializeField] private SpriteRenderer spriteRenderer;

        [Space]

        [SerializeField] private AnimClip[] animClips;

        [Space]

        [Range(PlaybackThreshold, 10f)]
        [SerializeField] private float playbackSpeed = 1f;

        public string CurrentAnimation => currentClip != null ? currentClip.name : string.Empty;
        public int CurrentFrames       => currentClip != null ? currentClip.Frames.Length : 0;
        public bool IsPlaying          => playing;

        private readonly Dictionary<string, AnimClip> clipMap = new();

        private AnimClip currentClip;

        private int currentFrame;
        private bool playing;

        private float timer;

        private void Awake()
        {
            foreach (var clip in animClips)
            {
                if (clip == null) continue;
                RegisterClip(clip);
            }
        }

        public void Play(string animName) => Play(animName, false);

        public void Play(string animName, bool forceReset)
        {
            if (!clipMap.TryGetValue(animName, out var clip))
            {
                Debug.LogWarning($"[SpriteAnimator] Clip not found: '{animName}' on {gameObject.name}");
                return;
            }

            AnimateClip(clip, forceReset);
        }

        public void Stop()
        {
            currentClip  = null;
            currentFrame = 0;
            timer        = 0f;
            playing      = false;
        }

        public void SetPlaybackSpeed(float speed)
        {
            if (speed < PlaybackThreshold)
                Debug.LogWarning($"Invalid playback speed. Speed must be greater than {PlaybackThreshold}");

            playbackSpeed = Mathf.Max(PlaybackThreshold, speed);
        }

        public void RegisterClip(AnimClip clip)
        {
            if (!clipMap.TryAdd(clip.name, clip))
                Debug.LogWarning($"[SpriteAnimator] Duplicate clip name: '{clip.name}' on {gameObject.name}");
        }

        public void UnRegisterClip(AnimClip clip)
        {
            if (clip == null)
            {
                Debug.LogError("Can't unregister a null clip");
                return;
            }

            if (clipMap.ContainsKey(clip.name))
                clipMap.Remove(clip.name);
        }

        private void Update()
        {
            if (currentClip == null || currentClip.Frames.Length == 0) 
                return;

            timer += Time.deltaTime * playbackSpeed;

            if (timer >= 1f / currentClip.FrameRate)
            {
                timer -= 1f / currentClip.FrameRate;
                currentFrame++;


                if (currentFrame >= currentClip.Frames.Length)
                {
                    UpdateSpriteRenderer(currentClip.Loop ? 0 : currentClip.Frames.Length - 1);
                    CallEvents(currentClip.name, currentClip.Loop);
                    return;
                }

                UpdateSpriteRenderer(currentFrame);
            }
        }

        private void AnimateClip(AnimClip clip, bool forceReset)
        {
            if (currentClip == clip && !forceReset) return;
            if (clip == null || clip.Frames.Length == 0) return;

            currentFrame = 0;
            currentClip  = clip;
            timer        = 0f;
            playing      = true;
            UpdateSpriteRenderer(0);
        }

        private void CallEvents(string clipName, bool loop)
        {
            if (loop)
            {
                AnimationLooped?.Invoke(clipName);
                return;
            }

            AnimationFinished?.Invoke(clipName);
            Stop();
        }

        private void UpdateSpriteRenderer(int frame)
        {
            currentFrame = frame;
            spriteRenderer.sprite = currentClip.Frames[currentFrame];
            FrameChanged?.Invoke(currentFrame);
        }
    }
}
