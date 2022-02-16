using System.Runtime.InteropServices;
using dotNES;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using UnityEngine;

namespace NESGame
{
    public class NAudioTest : MonoBehaviour
    {
        public string path= @"D:\CloudMusic\Honey.mp3";
        WaveOutEvent wave;

        public bool AsyncPlay;

        AudioBuffer m_buff;
        public void Awake()
        {
            m_buff = new AudioBuffer();
            var pd2 = new UnityNESAudio.NESWaveProvider(m_buff);
            var pd = new AudioFileReader(path);
            //var raw = new byte[48000 * 5];
            var freq = 500;
            var rate = 48000;
            var multiple = 2 * freq / (float)rate;
            var amplitude = 0.2f;
            for (int i = 0; i < rate*5; i++)
            {
                var sampleSaw = ((i * multiple) % 2f) - 1f;
                var sampleValue = sampleSaw;
                //var sample = (byte)(sampleValue * byte.MaxValue);
                //raw[i] = sample;
                m_buff.Write((byte)sampleValue);
            }
            
            wave = new WaveOutEvent();
            wave.Init(pd2);
            wave.Play();
        }
        public int size = 1024;
        public float val=1;

        public void Update()
        {
            //if (m_buff == null)
            //    return;
            //for (int i = 0; i < size; i++)
            //{
            //    m_buff.write(val);
            //}
            //if(wave.PlaybackState!=PlaybackState.Playing)
            //    wave.Play();
        }

        protected void OnDestroy()
        {
            wave?.Dispose();
        }
    }
}
