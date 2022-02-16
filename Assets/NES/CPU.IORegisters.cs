using System;
using System.Runtime.CompilerServices;

namespace dotNES
{
    sealed partial class CPU
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteIORegister(uint reg, byte val)
        {
            switch (reg)
            {
                case 0x4014: // OAM DMA
                    _emulator.PPU.PerformDMA(val);
                    break;
                case 0x4016:
                    if (_emulator.Controller != null)
                    {
                        foreach (var ctl in _emulator.Controller)
                        {
                            ctl.Strobe(val == 1);
                        }
                    }
                    break;
            }

            if (reg < 0x4014 || reg == 0x4015 || reg == 0x4017)
            //if (reg <= 0x401F)
            {
                _emulator.APU.Write(reg, val);
                return; // APU write
            }
            if (reg <= 0x401F)
            {
                return;// APU write
            }
            throw new NotImplementedException($"{reg.ToString("X4")} = {val.ToString("X2")}");
        }

        public uint ReadIORegister(uint reg)
        {
            switch (reg)
            {
                case 0x4016:
                    if (_emulator.Controller[0] != null)
                        return (uint)_emulator.Controller[0].ReadState() & 0x1;
                    break;
                case 0x4017:
                    if (_emulator.Controller[1] != null)
                        return (uint)_emulator.Controller[1].ReadState() & 0x1;
                    break;
            }
            if (reg < 0x4014 || reg==0x4015)
                _emulator.APU.Read(reg);
            return 0x00;
            //throw new NotImplementedException();
        }
    }
}
