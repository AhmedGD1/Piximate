using UnityEngine;

namespace Piximate
{
    [CreateAssetMenu(fileName = "AnimClip", menuName = "PixelCut/Animation Clip")]
    public class AnimClip : ScriptableObject
    {
        public Sprite[] Frames    => frames;
        public float    FrameRate => frameRate;
        public bool     Loop      => loop;

        [SerializeField] private Sprite[] frames;
        [SerializeField] private float    frameRate = 10;
        [SerializeField] private bool     loop      = true;

        public void SetFrames(Sprite[] frames)   => this.frames = frames;
        public void SetFrameRate(float frameRate) => this.frameRate = frameRate;
        public void SetLoop(bool loop)            => this.loop = loop;
    }
}