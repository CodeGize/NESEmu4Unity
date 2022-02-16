using System;
using System.IO;
using dotNES;
using ICSharpCode.SharpZipLib.Zip;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace NESGame
{
    public class UnityNes : MonoBehaviour
    {
        public UnityNESAudio Audio;
        public UnityNESController[] Controller;

        private Emulator m_Emulator;
        private Texture2D m_Tex;
        public RawImage Img;

        private const int GameWidth = 256;
        private const int GameHeight = 240;
        protected void Awake()
        {
            Application.targetFrameRate = -1;
            m_Tex = new Texture2D(GameWidth, GameHeight, TextureFormat.ARGB32, false);
            m_Tex.filterMode = FilterMode.Point;
            Img.texture = m_Tex;

        }

        public void SetTextureFilterMode(FilterMode mode)
        {
            m_Tex.filterMode = mode;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.pauseStateChanged += LogPauseState;
#endif
        }

        public float[] Ch;
        public bool[] ChEnables = new[] { true, true, true, true, true };

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.pauseStateChanged -= LogPauseState;
#endif
            if (m_Emulator == null)
                return;
            m_Emulator.Destroy();
            m_Emulator = null;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                m_Emulator?.Pause();
            else
                m_Emulator?.Resume();
        }

        private void OnDestroy()
        {
            if (m_Emulator == null)
                return;
            m_Emulator.Destroy();
            m_Emulator = null;
        }

        protected void FixedUpdate()
        {
            if (m_Emulator == null)
                return;
            //m_Emulator.AudioThreadRun();
            Ch = m_Emulator.APU.Ch;
        }

        [SerializeField]
        private int Cycles;
        protected void Update()
        {
            if (m_Emulator == null)
                return;
            var bitmap = m_Emulator.PPU.RawBitmap;
            m_Tex.SetPixelData(bitmap, 0);
            m_Tex.Apply();
        }

        #region 加载方法
        public void LoadRom(string path)
        {
            byte[] data;
            if (path.EndsWith(".zip"))
            {
                data = LoadZipRoom(path);
            }
            else
            {
                data = File.ReadAllBytes(path);
            }
            var rom = new UnityNESRom(path, data);

            m_Emulator = new Emulator(rom, Controller);
            m_Emulator.APU.SetAudio(Audio);
            m_Emulator.APU.ChEnable = ChEnables;
            m_Emulator.Start();
        }

        private byte[] LoadZipRoom(string path)
        {
            byte[] res = null;
            using (ZipInputStream s = new ZipInputStream(System.IO.File.OpenRead(path)))
            {
                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    if (theEntry.Name.EndsWith(".nes", StringComparison.CurrentCultureIgnoreCase))
                    {
                        res = new byte[theEntry.Size];
                        if (theEntry.Offset > 0)
                            s.Skip(theEntry.Offset);
                        s.Read(res, 0, res.Length);
                    }
                }
            }
            return res;
        }
        #endregion

#if UNITY_EDITOR
        private void LogPauseState(PauseState state)
        {
            switch (state)
            {
                case PauseState.Paused:
                    OnApplicationPause(true);
                    break;
                case PauseState.Unpaused:
                    OnApplicationPause(false);
                    break;
            }
        }
#endif
    }
}
