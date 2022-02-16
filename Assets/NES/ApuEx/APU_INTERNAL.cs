

using System;
using System.Diagnostics;

namespace dotNES
{

    class APU_INTERNAL
    {
        const int RECTANGLE_VOL_SHIFT = 8;
        const int TRIANGLE_VOL_SHIFT = 9;
        const int NOISE_VOL_SHIFT = 8;
        const int DPCM_VOL_SHIFT = 8;
        const float APU_CLOCK = 1789772.5f;

        float cpu_clock;
        int sampling_rate;
        int cycle_rate;

        protected RECTANGLE ch0;
        protected RECTANGLE ch1;
        protected TRIANGLE ch2;
        protected NOISE ch3;
        protected DPCM ch4;

        public APU_INTERNAL()
        {
            ch0 = new RECTANGLE();
            ch1 = new RECTANGLE();
            ch2 = new TRIANGLE();
            ch3 = new NOISE();
            ch4 = new DPCM();

            reg4015 = 0;

            cpu_clock = APU_CLOCK;
            sampling_rate = 22050;
            // 临时设定
            cycle_rate = (int)(cpu_clock * 65536.0f / 22050.0f);

        }

        public void SetParent(Emulator nes)
        {
            this.nes = nes;
        }

        public void Setup(float clock, int rate)
        {
            cpu_clock = clock;
            sampling_rate = rate;

            cycle_rate = (int)(clock * 65536.0f / rate);
        }

        public void Reset(float clock, int rate)
        {
            ch0 = new RECTANGLE();
            ch1 = new RECTANGLE();
            ch2 = new TRIANGLE();
            ch3 = new NOISE();
            ch4 = new DPCM();

            reg4015 = 0;

            // Sweep complement
            ch0.complement = 0x00;
            ch1.complement = 0xFF;

            // Noise shift register
            ch3.shift_reg = 0x4000;

            Setup(clock, rate);

            // $4011は初期化しない
            uint addr;
            for (addr = 0x4000; addr <= 0x4010; addr++)
            {
                Write(addr, 0x00);
                //SyncWrite(addr, 0x00);
            }
            //	Write( 0x4001, 0x08 );	// Reset時はincモードになる?
            //	Write( 0x4005, 0x08 );	// Reset時はincモードになる?
            Write(0x4012, 0x00);
            Write(0x4013, 0x00);
            Write(0x4015, 0x00);
        }

        public int Process(int channel)
        {
            switch (channel)
            {
                case 0:
                    return (int)RenderRectangle(ch0);
                case 1:
                    return (int)RenderRectangle(ch1);
                case 2:
                    return (int)RenderTriangle();
                case 3:
                    return (int)RenderNoise();
                case 4:
                    return (int)RenderDPCM();
            }
            return 0;
        }

        public byte Read(uint addr)//addr< 0x4014 || reg==0x4015
        {
            var data = addr >> 8;
            if (addr == 0x4015)
            {
                data = 0;
                if (ch0.enable && ch0.len_count > 0) data |= (1 << 0);
                if (ch1.enable && ch1.len_count > 0) data |= (1 << 1);
                if (ch2.enable > 0 && ch2.len_count > 0) data |= (1 << 2);
                if (ch3.enable > 0 && ch3.len_count > 0) data |= (1 << 3);
            }
            return (byte)data;
        }

        public void Write(uint addr, byte data)
        {
            switch (addr)
            {
                // CH0,1 rectangle
                case 0x4000:
                case 0x4001:
                case 0x4002:
                case 0x4003:
                case 0x4004:
                case 0x4005:
                case 0x4006:
                case 0x4007:
                    WriteRectangle((addr < 0x4004) ? 0 : 1, addr, data);
                    break;
                // CH2 triangle
                case 0x4008:
                case 0x4009:
                case 0x400A:
                case 0x400B:
                    WriteTriangle(addr, data);
                    break;
                // CH3 noise
                case 0x400C:
                case 0x400D:
                case 0x400E:
                case 0x400F:
                    WriteNoise(addr, data);
                    break;

                // CH4 DPCM
                case 0x4010:
                case 0x4011:
                case 0x4012:
                case 0x4013:
                    WriteDPCM(addr, data);
                    break;

                case 0x4015:
                    reg4015 = data;
                    UpdateChStatus(data);

                    break;

                case 0x4017:
                    break;

                // VirtuaNES固有ポート
                case 0x4018:
                    UpdateRectangle(ch0, data);
                    UpdateRectangle(ch1, data);
                    UpdateTriangle(data);
                    UpdateNoise(data);
                    break;

            }
        }

        private void UpdateNoise(byte type)
        {
            if (ch3.enable == 0 || ch3.len_count <= 0)
                return;

            // Update Length
            if (ch3.holdnote == 0)
            {
                // Holdnote
                if ((type & 1) == 0 && ch3.len_count > 0)
                {
                    ch3.len_count--;
                }
            }

            // Update Envelope
            if (ch3.env_count > 0)
            {
                ch3.env_count--;
            }
            if (ch3.env_count == 0)
            {
                ch3.env_count = ch3.env_decay;

                // Holdnote
                if (ch3.holdnote > 0)
                {
                    ch3.env_vol = (ch3.env_vol - 1) & 0x0F;
                }
                else if (ch3.env_vol > 0)
                {
                    ch3.env_vol--;
                }
            }

            if (ch3.env_fixed == 0)
            {
                ch3.nowvolume = ch3.env_vol << RECTANGLE_VOL_SHIFT;
            }
        }

        private void UpdateTriangle(byte type)
        {
            if (ch2.enable > 0)
                return;

            if ((type & 1) == 0 && ch2.holdnote == 0)
            {
                if (ch2.len_count > 0)
                {
                    ch2.len_count--;
                }
            }

            //	if( !ch2.len_count ) {
            //		ch2.lin_count = 0;
            //	}

            // Update Length/Linear
            if (ch2.counter_start > 0)
            {
                ch2.lin_count = ch2.reg[0] & 0x7F;
            }
            else if (ch2.lin_count > 0)
            {
                ch2.lin_count--;
            }
            if (ch2.holdnote == 0 && ch2.lin_count > 0)
            {
                ch2.counter_start = 0;
            }
        }

        private void UpdateRectangle(RECTANGLE ch, byte type)
        {
            if (!ch.enable || ch.len_count <= 0)
                return;

            // Update Length/Sweep
            if ((type & 1) == 0)
            {
                // Update Length
                if (ch.len_count > 0 && ch.holdnote == 0)
                {
                    // Holdnote
                    if (ch.len_count > 0)
                    {
                        ch.len_count--;
                    }
                }

                // Update Sweep
                if (ch.swp_on > 0 && ch.swp_shift > 0)
                {
                    if (ch.swp_count > 0)
                    {
                        ch.swp_count--;
                    }
                    if (ch.swp_count == 0)
                    {
                        ch.swp_count = ch.swp_decay;
                        if (ch.swp_inc > 0)
                        {
                            // Sweep increment(to higher frequency)
                            if (ch.complement == 0)
                                ch.freq += ~(ch.freq >> ch.swp_shift); // CH 0
                            else
                                ch.freq -= (ch.freq >> ch.swp_shift); // CH 1
                        }
                        else
                        {
                            // Sweep decrement(to lower frequency)
                            ch.freq += (ch.freq >> ch.swp_shift);
                        }
                    }
                }
            }


            if (ch.env_fixed == 0)
            {
                ch.nowvolume = ch.env_vol << RECTANGLE_VOL_SHIFT;
            }
        }

        int[] dpcm_cycles_pal = {
            397, 353, 315, 297, 265, 235, 209, 198,
    176, 148, 131, 118,  98,  78,  66,  50
};

        // DMC 転送クロック数テーブル
        int[] dpcm_cycles = {
            428, 380, 340, 320, 286, 254, 226, 214,
    190, 160, 142, 128, 106,  85,  72,  54
        };

        private void WriteDPCM(uint addr, byte data)
        {
            ch4.reg[addr & 3] = data;
            switch (addr & 3)
            {
                case 0:
                    //ch4.freq = INT2FIX(nes->GetVideoMode() ? dpcm_cycles_pal[data & 0x0F] : dpcm_cycles[data & 0x0F]);
                    ch4.freq = INT2FIX(dpcm_cycles[data & 0x0F]);
                    ////			ch4.freq    = INT2FIX( (dpcm_cycles[data&0x0F]-((data&0x0F)^0x0F)*2-2) );
                    ch4.looping = (data & 0x40) > 0;
                    break;
                case 1:
                    ch4.dpcm_value = (byte)((data & 0x7F) >> 1);
                    break;
                case 2:
                    ch4.cache_addr = (ushort)(0xC000 + (data << 6));
                    break;
                case 3:
                    ch4.cache_dmalength = ((data << 4) + 1) << 3;
                    break;
            }
        }

        private int[] noise_freq = {
            4,    8,   16,   32,   64,   96,  128,  160,
            202,  254,  380,  508,  762, 1016, 2034, 4068
        };

        private int[] vbl_length = {
            5, 127,   10,   1,   19,   2,   40,   3,
            80,   4,   30,   5,    7,   6,   13,   7,
            6,   8,   12,   9,   24,  10,   48,  11,
            96,  12,   36,  13,    8,  14,   16,  15
        };

        private void WriteNoise(uint addr, byte data)
        {
            ch3.reg[addr & 3] = data;
            switch (addr & 3)
            {
                case 0:
                    ch3.holdnote = (byte)(data & 0x20);
                    ch3.volume = (byte)(data & 0x0F);
                    ch3.env_fixed = (byte)(data & 0x10);
                    ch3.env_decay = (byte)((data & 0x0F) + 1);
                    break;
                case 1: // Unused
                    break;
                case 2:
                    ch3.freq = INT2FIX(noise_freq[data & 0x0F]);
                    ch3.xor_tap = (byte)((data & 0x80) > 0 ? 0x40 : 0x02);
                    break;
                case 3: // Master
                    ch3.len_count = vbl_length[data >> 3] * 2;
                    ch3.env_vol = 0x0F;
                    ch3.env_count = (byte)(ch3.env_decay + 1);

                    if ((reg4015 & (1 << 3)) > 0)
                        ch3.enable = 0xFF;
                    break;
            }
        }

        private void WriteTriangle(uint addr, byte data)
        {
            ch2.reg[addr & 3] = data;
            switch (addr & 3)
            {
                case 0:
                    ch2.holdnote = data & 0x80;
                    break;
                case 1: // Unused
                    break;
                case 2:
                    ch2.freq = INT2FIX((((ch2.reg[3] & 0x07) << 8) + data + 1));
                    break;
                case 3: // Master
                    ch2.freq = INT2FIX((((data & 0x07) << 8) + ch2.reg[2] + 1));
                    ch2.len_count = vbl_length[data >> 3] * 2;
                    ch2.counter_start = 0x80;

                    if ((reg4015 & (1 << 2)) > 0)
                        ch2.enable = 0xFF;
                    break;
            }
        }

        int[] duty_lut = {
            2,  4,  8, 12
        };

        int[] freq_limit = {
            0x03FF, 0x0555, 0x0666, 0x071C, 0x0787, 0x07C1, 0x07E0, 0x07F0
        };

        private void WriteRectangle(int no, uint addr, byte data)
        {
            var ch = (no == 0) ? ch0 : ch1;

            ch.reg[addr & 3] = data;
            switch (addr & 3)
            {
                case 0:
                    //ddle nnnn   duty, loop env/disable length, env disable, vol/env period
                    ch.duty = duty_lut[data >> 6];
                    ch.holdnote = (byte)(data & 0x20);
                    ch.env_fixed = (byte)(data & 0x10);
                    ch.volume = (byte)(data & 0x0F);
                    //ch.env_decay = (byte)((data & 0x0F) + 1);//?
                    break;
                case 1:
                    //eppp nsss   enable sweep, period, negative, shift
                    ch.swp_on = (byte)(data & 0x80);
                    ch.swp_inc = (byte)(data & 0x08);
                    ch.swp_shift = (byte)(data & 0x07);
                    //ch.swp_decay = (byte)(((data >> 4) & 0x07) + 1);
                    ch.freqlimit = freq_limit[data & 0x07];
                    break;
                case 2:
                    //pppp pppp   period low
                    ch.freq = (ch.freq & (~0xFF)) + data;
                    break;
                case 3: // Master
                    //llll lppp   length index, period high
                    ch.freq = ((data & 0x07) << 8) + (ch.freq & 0xFF);
                    ch.len_count = vbl_length[data >> 3] * 2;
                    ch.env_vol = 0x0F;
                    //ch.env_count = (byte)(ch.env_decay + 1);
                    ch.adder = 0;

                    if ((reg4015 & (1 << no)) > 0)
                        ch.enable = true;
                    break;
            }
        }

        private int reg4015;
        private void UpdateChStatus(int data)
        {
            if ((data & (1 << 0)) == 0)
            {
                ch0.enable = false;
                ch0.len_count = 0;
            }
            if ((data & (1 << 1)) == 0)
            {
                ch1.enable = false;
                ch1.len_count = 0;
            }
            if ((data & (1 << 2)) == 0)
            {
                ch2.enable = 0;
                ch2.len_count = 0;
                ch2.lin_count = 0;
                ch2.counter_start = 0;
            }
            if ((data & (1 << 3)) == 0)
            {
                ch3.enable = 0;
                ch3.len_count = 0;
            }
            if ((data & (1 << 4)) == 0)
            {
                ch4.enable = 0;
                ch4.dmalength = 0;
            }
            else
            {
                ch4.enable = 0xFF;
                if (ch4.dmalength == 0)
                {
                    ch4.address = ch4.cache_addr;
                    ch4.dmalength = ch4.cache_dmalength;
                    ch4.phaseacc = 0;
                }
            }
        }

        private Emulator nes;
        private float RenderDPCM()
        {
            if (ch4.dmalength > 0)
            {
                ch4.phaseacc -= cycle_rate;

                while (ch4.phaseacc < 0)
                {
                    ch4.phaseacc += ch4.freq;
                    if ((ch4.dmalength & 7) == 0)
                    {
                        ch4.cur_byte = (byte)nes.CPU.ReadByte(ch4.address);
                        if (0xFFFF == ch4.address)
                            ch4.address = 0x8000;
                        else
                            ch4.address++;
                    }

                    if ((--ch4.dmalength) <= 0)
                    {
                        if (ch4.looping)
                        {
                            ch4.address = ch4.cache_addr;
                            ch4.dmalength = ch4.cache_dmalength;
                        }
                        else
                        {
                            ch4.enable = 0;
                            break;
                        }
                    }
                    // positive delta
                    if ((ch4.cur_byte & (1 << ((ch4.dmalength & 7) ^ 7))) > 0)
                    {
                        if (ch4.dpcm_value < 0x3F)
                            ch4.dpcm_value += 1;
                    }
                    else
                    {
                        // negative delta
                        if (ch4.dpcm_value > 1)
                            ch4.dpcm_value -= 1;
                    }
                }
            }
            ch4.output = ((ch4.reg[1] & 0x01) + ch4.dpcm_value * 2) << DPCM_VOL_SHIFT;
            return ch4.output;
        }

        private float RenderNoise()
        {
            if (ch3.enable == 0 || ch3.len_count <= 0)
                return 0;

            if (ch3.env_fixed > 0)
            {
                ch3.nowvolume = ch3.volume << NOISE_VOL_SHIFT;
            }

            var vol = 256 - ((ch4.reg[1] & 0x01) + ch4.dpcm_value * 2);
            var cycle_rate = 5319480;
            ch3.phaseacc -= cycle_rate;
            if (ch3.phaseacc >= 0)
                return ch3.output * (vol / 256f);

            if (ch3.freq > cycle_rate)
            {
                ch3.phaseacc += ch3.freq;
                if (NoiseShiftreg((byte)ch3.xor_tap))
                    ch3.output = ch3.nowvolume;
                else
                    ch3.output = -ch3.nowvolume;

                return ch3.output * (vol / 256f);
            }

            int num_times, total;
            num_times = total = 0;
            while (ch3.phaseacc < 0 && ch3.freq > 0)
            {
                ch3.phaseacc += ch3.freq;
                if (NoiseShiftreg((byte)ch3.xor_tap))
                    ch3.output = ch3.nowvolume;
                else
                    ch3.output = -ch3.nowvolume;

                total += ch3.output;
                num_times++;
            }
            if (num_times > 0)
                return (total / num_times) * (vol / 256f);
            return ch3.output * (vol / 256f);
        }

        private float RenderTriangle()
        {
            int vol;
            if (ConfigWrapper.GetCCfgSound().bDisableVolumeEffect)
                vol = 256;
            else
                vol = 256 - ((ch4.reg[1] & 0x01) + ch4.dpcm_value * 2);

            if (ch2.enable == 0 || (ch2.len_count <= 0) || (ch2.lin_count <= 0))
            {
                return ch2.nowvolume * (vol / 256f);
            }

            if (ch2.freq < INT2FIX(8))
            {
                return ch2.nowvolume * (vol / 256f);
            }

            ch2.phaseacc -= cycle_rate;
            if (ch2.phaseacc >= 0)
            {
                return ch2.nowvolume * (vol / 256f);
            }
            if (ch2.freq > cycle_rate)
            {
                ch2.phaseacc += ch2.freq;
                ch2.adder = (ch2.adder + 1) & 0x1F;

                if (ch2.adder < 0x10)
                {
                    ch2.nowvolume = (ch2.adder & 0x0F) << TRIANGLE_VOL_SHIFT;
                }
                else
                {
                    ch2.nowvolume = (0x0F - (ch2.adder & 0x0F)) << TRIANGLE_VOL_SHIFT;
                }

                return ch2.nowvolume * (vol / 256f);
            }
            // 加重平均
            int num_times, total;
            num_times = total = 0;
            while (ch2.phaseacc < 0)
            {
                ch2.phaseacc += ch2.freq;
                ch2.adder = (ch2.adder + 1) & 0x1F;

                if (ch2.adder < 0x10)
                {
                    ch2.nowvolume = (ch2.adder & 0x0F) << TRIANGLE_VOL_SHIFT;
                }
                else
                {
                    ch2.nowvolume = (0x0F - (ch2.adder & 0x0F)) << TRIANGLE_VOL_SHIFT;
                }

                total += ch2.nowvolume;
                num_times++;
            }
            return (total / num_times) * (vol / 256f);
        }

        public int INT2FIX(int x)
        {
            return x << 16;
        }

        // Noise ShiftRegister
        bool NoiseShiftreg(byte xor_tap)
        {
            int bit0, bit14;

            bit0 = ch3.shift_reg & 1;
            if ((ch3.shift_reg & xor_tap) > 0)
                bit14 = bit0 ^ 1;
            else bit14 = bit0 ^ 0;
            ch3.shift_reg >>= 1;
            ch3.shift_reg |= (bit14 << 14);
            return (bit0 ^ 1) > 0;
        }

        private float RenderRectangle(RECTANGLE ch)
        {
            if (!ch.enable || ch.len_count <= 0)
                return 0;
            if (ch.freq < 8 || (ch.swp_inc == 0 && ch.freq > ch.freqlimit))
                return 0;
            if (ch.env_fixed > 0)
                ch.nowvolume = ch.volume << RECTANGLE_VOL_SHIFT;
            var volume = ch.nowvolume;

            //补间处理
            int sample_weight = ch.phaseacc;
            if (sample_weight > cycle_rate)
                sample_weight = cycle_rate;

            var total = (ch.adder < ch.duty) ? sample_weight : -sample_weight;

            var freq = (ch.freq + 1) << 16;
            ch.phaseacc -= cycle_rate;
            while (ch.phaseacc < 0)
            {
                ch.phaseacc += freq;
                ch.adder = (ch.adder + 1) & 0x0F;

                sample_weight = freq;
                if (ch.phaseacc > 0)
                {
                    sample_weight -= ch.phaseacc;
                }
                total += (ch.adder < ch.duty) ? sample_weight : -sample_weight;
            }
            var res = volume * (total / cycle_rate) + 0.5f;
            return res;
        }
    }

    class DPCM
    {
        public byte[] reg = new byte[4];
        public byte enable;
        public bool looping;
        public byte cur_byte;
        public byte dpcm_value;

        public int freq;
        public int phaseacc;
        public int output;

        public ushort address, cache_addr;
        public int dmalength, cache_dmalength;
        public int dpcm_output_real, dpcm_output_fake, dpcm_output_old, dpcm_output_offset;
    }

    class NOISE
    {
        public byte[] reg = new byte[4];        // register

        public int enable;        // enable
        public byte holdnote;  // holdnote
        public byte volume;        // volume
        public byte xor_tap;
        public int shift_reg;

        // For Render
        public int phaseacc;
        public int freq;
        public int len_count;

        public int nowvolume;
        public int output;

        // For Envelope
        public byte env_fixed;
        public byte env_decay;
        public byte env_count;
        //public int dummy0;
        public int env_vol;
    }

    class TRIANGLE
    {
        public byte[] reg = new byte[4];

        public byte enable;
        public int holdnote;
        public byte counter_start;
        //public byte dummy0;

        public int phaseacc;
        public int freq;
        public int len_count;
        public int lin_count;
        public int adder;

        public int nowvolume;

        //// For sync;
        //public byte[] sync_reg = new byte[4];
        //public byte sync_enable;
        //public byte sync_holdnote;
        //public byte sync_counter_start;
        ////		byte	dummy1;
        //public int sync_len_count;
        //public int sync_lin_count;
    }

    /// <summary>
    /// 方形波
    /// </summary>
    class RECTANGLE
    {
        public byte[] reg = new byte[4];
        public bool enable;
        public byte holdnote;
        public byte volume;
        public byte complement;

        // For Render
        public int phaseacc;
        public int freq;
        public int freqlimit;
        public int adder;
        public int duty;
        public int len_count;

        public int nowvolume;

        // For Envelope
        public byte env_fixed;
        //public byte dummy0;
        public int env_vol;

        // For Sweep
        public byte swp_on;
        public byte swp_inc;
        public byte swp_shift;
        public byte swp_decay;
        public byte swp_count;
        //public byte[] dummy1 = new byte[3];
    }
}
