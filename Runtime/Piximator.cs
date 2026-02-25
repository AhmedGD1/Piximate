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
        private bool animationFinished;

        private float timer;

        private void Awake()
        {
            foreach (var clip in animClips)
            {
                if (clip == null) continue;
                RegisterClip(clip);
            }
        }

        public void Play(string animName = "")
        {
            if (string.IsNullOrEmpty(animName))
            {
                // Resume current/last clip from current frame
                if (currentClip != null)
                {
                    if (animationFinished)
                    {
                        currentFrame      = 0;
                        timer             = 0f;
                        animationFinished = false;
                    }
                    playing = true;
                }
                return;
            }

            if (!clipMap.TryGetValue(animName, out var clip))
            {
                Debug.LogWarning($"[SpriteAnimator] Clip not found: '{animName}' on {gameObject.name}");
                return;
            }

            // Same clip — restart if non-looping, continue if looping
            if (currentClip == clip)
            {
                if (!currentClip.Loop)
                {
                    currentFrame      = 0;
                    timer             = 0f;
                    animationFinished = false;
                }
                playing = true;
                return;
            }

            // New clip — always restart
            currentClip       = clip;
            currentFrame      = 0;
            timer             = 0f;
            animationFinished = false;
            playing           = true;
            UpdateSpriteRenderer(0);
        }

        public void PlayBackwards(string animName = "")
        {
            Play(animName);
            playbackSpeed = -Mathf.Abs(playbackSpeed);
        }

        public void Stop()
        {
            playing          = false;
            animationFinished = false;
            // Frame and clip intentionally preserved so Play() can resume
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
            if (!playing || currentClip == null || currentClip.Frames.Length == 0)
                return;

            timer += Time.deltaTime * playbackSpeed;
            float frameDuration = 1f / currentClip.FrameRate;

            if (Mathf.Abs(timer) >= frameDuration)
            {
                timer -= Mathf.Sign(timer) * frameDuration;
                currentFrame += playbackSpeed > 0 ? 1 : -1;

                if (currentFrame >= currentClip.Frames.Length || currentFrame < 0)
                {
                    int boundaryFrame = playbackSpeed > 0 ? currentClip.Frames.Length - 1 : 0;
                    UpdateSpriteRenderer(currentClip.Loop ? (playbackSpeed > 0 ? 0 : currentClip.Frames.Length - 1) : boundaryFrame);
                    CallEvents(currentClip.name, currentClip.Loop);
                    return;
                }

                UpdateSpriteRenderer(currentFrame);
            }
        }

        private void CallEvents(string clipName, bool loop)
        {
            if (loop)
            {
                AnimationLooped?.Invoke(clipName);
                return;
            }

            animationFinished = true;
            playing           = false;
            AnimationFinished?.Invoke(clipName);
        }

        private void UpdateSpriteRenderer(int frame)
        {
            currentFrame          = frame;
            spriteRenderer.sprite = currentClip.Frames[currentFrame];
            FrameChanged?.Invoke(currentFrame);
        }
    }
}
