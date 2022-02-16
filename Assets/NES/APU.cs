using System;
using System.Linq;

namespace dotNES
{
    public interface IAudioPlayer
    {
        void Init(AudioBuffer audioBuffer);
    }

    sealed partial class APU
    {
        private IAudioPlayer AudioPlayer;

        private AudioBuffer audioBuffer;

        public void SetAudio(IAudioPlayer player)
        {
            AudioPlayer = player;
            AudioPlayer.Init(audioBuffer);
        }
        Emulator Emulator;
        public APU(Emulator emulator)
        {
            Emulator = emulator;
            audioBuffer = new AudioBuffer();
            APU_INTERNAL.SetParent(Emulator);
        }

        APU_INTERNAL APU_INTERNAL = new APU_INTERNAL();

        
        public float[] Ch = new float[5];
        public bool[] ChEnable = new bool[5];

        private float m_lastCycle;
        public void ProcessFrame(int cs)
        {
            var steps = cs;
            var delta = (Emulator.CPU.TotleCycle - m_lastCycle) / (float)steps;
            if (delta <= 0)
                return;
            QueueData data;
            var time = m_lastCycle;
            m_lastCycle = Emulator.CPU.TotleCycle;
            while (steps > 0)
            {
                steps--;

                var cycles = time;
                var t = 0;
                while (GetQueue(cycles, out data))
                {
                    t++;
                    WriteProcess(data.addr, data.data);
                }
                //if (t > 1)
                //    UnityEngine.Debug.Log("Write more than 1:" + t);


                if (ChEnable[0]) Ch[0] = APU_INTERNAL.Process(0);// * RECTANGLE_VOL >> 8;
                if (ChEnable[1]) Ch[1] = APU_INTERNAL.Process(1);// * RECTANGLE_VOL >> 8;
                if (ChEnable[2]) Ch[2] = APU_INTERNAL.Process(2);// * TRIANGLE_VOL >> 8;
                if (ChEnable[3]) Ch[3] = APU_INTERNAL.Process(3)-6400;// * NOISE_VOL >> 8;
                if (ChEnable[4])
                {
                    Ch[4] = APU_INTERNAL.Process(4);// * DPCM_VOL >> 8;
                    if (Ch[4] <= 6400)
                        Ch[4] = 0;
                }
                var output = Ch.Sum();

                // Limit
                if (output > 0x7FFF)
                {
                    output = 0x7FFF;
                }
                else if (output < -0x8000)
                {
                    output = -0x8000;
                }

                var a = (int)output;

                //UnityEngine.Debug.Log(a);

                var h = a >> 8;
                var l = a & 0xF;

                audioBuffer.Write((byte)l);
                audioBuffer.Write((byte)h);

                time += delta;
                if (time > Emulator.CPU.TotleCycle)
                    time = Emulator.CPU.TotleCycle;
            }
        }

        private void WriteProcess(uint addr, byte data)
        {
            if (addr >= 0x4000 && addr <= 0x401F)
            {
                APU_INTERNAL.Write(addr, data);
            }
        }

        internal int Read(uint reg)//reg < 0x4014 || reg==0x4015
        {
            return APU_INTERNAL.Read(reg);
        }

        internal void Write(uint reg, byte val)//0x4000<reg<0x4014||reg==0x4015||reg==0x4017 子线程
        {
            var addr = reg;
            if (addr >= 0x4000 && addr <= 0x401F)
            {
                //WriteProcess(addr, val);
                var time = (int)Emulator.CPU.TotleCycle;
                SetQueue(time, addr, val);
            }
        }

        const int RECTANGLE_VOL = (0x0F0);//240
        const int TRIANGLE_VOL = (0x130);//304
        const int NOISE_VOL = (0x0C0);//192
        const int DPCM_VOL = (0x0F0);//240
    }

    public class AudioBuffer
    {
        byte[] audioRingBuffer = new byte[1 << 16];
        ushort startPointer = 0;
        ushort nextSamplePointer = 0;

        public void Write(byte value)
        {
            audioRingBuffer[nextSamplePointer] = value;
            nextSamplePointer++;
        }

        public int Read(byte[] audioBuff, int offset, int count)
        {
            if (startPointer < nextSamplePointer)
            {
                ushort amountToCopy = (ushort)Math.Min(nextSamplePointer - startPointer, count);
                Array.Copy(audioRingBuffer, startPointer, audioBuff, offset, amountToCopy);
                startPointer += amountToCopy;
                return amountToCopy;
            }
            else if (nextSamplePointer < startPointer)
            {
                int amountAfter = Math.Min(audioRingBuffer.Length - startPointer, count);
                Array.Copy(audioRingBuffer, startPointer, audioBuff, offset, amountAfter);
                count -= amountAfter;

                int amountBefore = Math.Min(nextSamplePointer, count);
                Array.Copy(audioRingBuffer, 0, audioBuff, offset + amountAfter, amountBefore);
                int floatsCopied = amountAfter + amountBefore;
                startPointer += (ushort)floatsCopied;
                return floatsCopied;
            }
            else
            {
                return 0;
            }
        }
    }
}
