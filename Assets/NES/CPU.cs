using System;
using System.Linq;
using System.Reflection;

namespace dotNES
{
    /// <summary>
    /// 6502 Chip Cpu
    /// </summary>
    sealed partial class CPU : Addressable
    {
        private readonly byte[] _ram = new byte[0x800];//2K内存
        public int Cycle;//执行循环
        private uint _currentInstruction;//当前指令

        public delegate void Opcode();

        private readonly Opcode[] _opcodes = new Opcode[256];//指令操作
        private readonly string[] _opcodeNames = new string[256];//指令名称
        private readonly OpcodeDef[] _opcodeDefs = new OpcodeDef[256];//指令定义

        public CPU(Emulator emulator) : base(emulator, 0xFFFF)
        {
            InitializeOpcodes();
            InitializeMemoryMap();
            Initialize();
        }

        private void InitializeOpcodes()
        {
            var opcodeBindings = from opcode in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                                 let defs = opcode.GetCustomAttributes(typeof(OpcodeDef), false)
                                 where defs.Length > 0
                                 select new
                                 {
                                     binding = (Opcode)Delegate.CreateDelegate(typeof(Opcode), this, opcode.Name),
                                     name = opcode.Name,
                                     defs = (from d in defs select (OpcodeDef)d)
                                 };

            foreach (var opcode in opcodeBindings)
            {
                foreach (var def in opcode.defs)
                {
                    _opcodes[def.Opcode] = opcode.binding;
                    _opcodeNames[def.Opcode] = opcode.name;
                    this._opcodeDefs[def.Opcode] = def;
                }
            }
        }

        //public void Execute()
        //{
        //    for (int i = 0; i < 5000; i++)
        //    {
        //        ExecuteSingleInstruction();
        //    }


        //    //uint w;
        //    //ushort x = 6000;
        //    //string z = "";
        //    //while ((w = ReadByte(x)) != '\0')
        //    //{
        //    //    z += (char)w;
        //    //}

        //    //Console.WriteLine(">>> " + ReadByte(0x02));
        //}
    }
}
