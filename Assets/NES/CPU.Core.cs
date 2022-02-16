using System;
using System.Windows.Markup;

namespace dotNES
{

    sealed partial class CPU
    {
        public enum InterruptType
        {
            NMI, IRQ, RESET
        }

        private readonly uint[] _interruptHandlerOffsets = { 0xFFFA, 0xFFFE, 0xFFFC };
        private readonly bool[] _interrupts = new bool[2];//只需要处理NMI和IRQ

        public void Initialize()//reset
        {
            A = 0;
            X = 0;
            Y = 0;
            SP = 0xFD;
            P = 0x24;

            PC = ReadWord(_interruptHandlerOffsets[(int)InterruptType.RESET]);
        }

        public void Reset()//?
        {
            SP -= 3;
            F.InterruptsDisabled = true;
        }

        public void TickFromPPU()
        {
            //if (Cycle-- > 0) return;
            ExecuteSingleInstruction();
        }
        /// <summary>
        /// 总循环次数
        /// </summary>
        public long TotleCycle { get; private set; }
        public void ExecuteSingleInstruction()//执行单指令
        {
            TotleCycle++;
            if (Cycle > 0)
            {
                Cycle--;
                return;
            }

            for (int i = 0; i < _interrupts.Length; i++)//中断处理
            {
                if (_interrupts[i])
                {
                    PushWord(PC);
                    Push(P);
                    PC = ReadWord(_interruptHandlerOffsets[i]);
                    F.InterruptsDisabled = true;
                    _interrupts[i] = false;
                    Cycle += 7;
                    return;
                }
            }

            _currentInstruction = NextByte();

            Cycle += _opcodeDefs[_currentInstruction].Cycles;

            ResetInstructionAddressingMode();
            // if (_numExecuted > 10000 && PC - 1 == 0xFF61)
            //  if(_emulator.Controller.debug || 0x6E00 <= PC && PC <= 0x6EEF)
            //      Console.WriteLine($"{(PC - 1).ToString("X4")}  {_currentInstruction.ToString("X2")}	{opcodeNames[_currentInstruction]}\t\t\tA:{A.ToString("X2")} X:{X.ToString("X2")} Y:{Y.ToString("X2")} P:{P.ToString("X2")} SP:{SP.ToString("X2")}");

            Opcode op = _opcodes[_currentInstruction];
            if (op == null)
                throw new ArgumentException(_currentInstruction.ToString("X2"));
            op();
        }

        public void TriggerInterrupt(InterruptType type)//中断
        {
            //if (F.InterruptsDisabled)
            //    return;
            switch (type)
            {
                case InterruptType.NMI:
                case InterruptType.IRQ:
                    _interrupts[(int)type] = true;
                    break;
            }
        }
    }
}
