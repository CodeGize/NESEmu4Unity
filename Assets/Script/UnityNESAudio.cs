using dotNES;
#if UNITY_EDITOR_WIN
using NAudio.Wave;
#endif
using UnityEngine;

namespace NESGame
{
    public class UnityNESAudio : MonoBehaviour, IAudioPlayer
    {
#if UNITY_EDITOR_WIN
        private IWavePlayer waveOut;

        [SerializeField]
        private bool IsPlaying;
        private void Awake()
        {
            waveOut = new WaveOutEvent();
        }

        public void Start()
        {

        }

        public void Update()
        {
            if (m_inited)
            {
                if (waveOut.PlaybackState != PlaybackState.Playing)
                    waveOut.Play();
            }
        }

        private bool m_inited;
        public void Init(AudioBuffer audioBuffer)
        {
            var nesWaveProvider = new NESWaveProvider(audioBuffer);

            var cfg = ConfigWrapper.GetCCfgSound();
            var bits = cfg.nBits;
            var rate = cfg.nRate;
            nesWaveProvider.SetWaveFormat(rate, 16, 1);
            waveOut.Init(nesWaveProvider);
            m_inited = true;
        }

        protected void OnDestroy()
        {
            waveOut?.Dispose();
        }

        public class NESWaveProvider : IWaveProvider
        {
            private WaveFormat waveFormat;
            private AudioBuffer audioBuffer;

            public NESWaveProvider(AudioBuffer audioBuffer)
                : this(48000, 8, 1)
            {
                this.audioBuffer = audioBuffer;
            }

            public NESWaveProvider(int sampleRate, int bits, int channels)
            {
                SetWaveFormat(sampleRate, bits, channels);
            }

            public void SetWaveFormat(int sampleRate, int bits, int channels)
            {
                waveFormat = new WaveFormat(sampleRate, bits, channels);
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                WaveBuffer waveBuffer = new WaveBuffer(buffer);
                return Read2(waveBuffer.ByteBuffer, offset , count);
            }

            private int Read2(byte[] buffer, int offset, int sampleCount)
            {
                return audioBuffer.Read(buffer, offset, sampleCount);
            }

            public WaveFormat WaveFormat
            {
                get { return waveFormat; }
            }

        }
#else
        public void Play(){}
        public void Init(AudioBuffer audioBuffer){}
#endif
    }


}
