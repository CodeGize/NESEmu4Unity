using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using dotNES.Controllers;
using dotNES.Mappers;

namespace dotNES
{
    class Emulator
    {
        private static readonly Dictionary<int, KeyValuePair<Type, MapperDef>> Mappers = (
            from type in Assembly.GetExecutingAssembly().GetTypes()
            let def = (MapperDef)type.GetCustomAttributes(typeof(MapperDef), true).FirstOrDefault()
            where def != null
            select new { def, type }).ToDictionary(a => a.def.Id, a => new KeyValuePair<Type, MapperDef>(a.type, a.def));

        public IController[] Controller;

        public readonly CPU CPU;

        public readonly PPU PPU;

        public readonly APU APU;

        public readonly BaseMapper Mapper;

        public readonly Cartridge Cartridge;

        private readonly string _path;

        public Emulator(IRom rom, IController[] controller)
        {
            _path = rom.Path;
            Cartridge = new Cartridge(rom.Data);
            if (!Mappers.ContainsKey(Cartridge.MapperNumber))
                throw new NotImplementedException($"unsupported mapper {Cartridge.MapperNumber}");
            Mapper = (BaseMapper)Activator.CreateInstance(Mappers[Cartridge.MapperNumber].Key, this);
            CPU = new CPU(this);
            PPU = new PPU(this);
            APU = new APU(this);
            Controller = controller;

            Load();
        }

        public bool IsPause { get; private set; }
        public void Pause()
        {
            IsPause = true;
        }

        public void Resume()
        {
            IsPause = false;
        }


        public void Start()
        {
            IsRunning = true;
            var thread1 = new Thread(ThreadRun);
            thread1.Start();
            var thread2 = new Thread(AudioThreadRun);
            thread2.Start();
        }

        public void AudioThreadRun()
        {
            while (true)
            {
                if (IsPause)
                    continue;
                if (APU != null)
                {
                    try
                    {
                        var cfg = ConfigWrapper.GetCCfgSound();
                        var samplerate = cfg.nRate;//频率,1秒采样次数
                        var blocksize = cfg.nBits / 8;
                        //var audioFPS = 60;//音频fps
                        var time = 20f;//ms
                        var cs = time * samplerate / 1000f;
                        Thread.Sleep((int)time);
                        APU.ProcessFrame((int)cs);
                    }catch(Exception e)
                    {
                        throw e;
                    }
                }
            }
        }

        public bool IsRunning { get; private set; }

        public void Destroy()
        {
            IsRunning = false;
        }

        private void ThreadRun(object state)
        {
            while (IsRunning)
            {
                if (IsPause)
                    continue;

                if (PPU != null)
                {
                    PPU.ProcessFrame();
                }

            }
        }

        public void Save()
        {
            using (var fs = new FileStream(_path + ".sav", FileMode.Create, FileAccess.Write))
            {
                Mapper.Save(fs);
            }
        }

        public void Load()
        {
            var sav = _path + ".sav";
            if (!File.Exists(sav)) return;

            using (var fs = new FileStream(sav, FileMode.Open, FileAccess.Read))
            {
                Mapper.Load(fs);
            }
        }
    }

    public interface IRom
    {
        string Path { get; }
        byte[] Data { get; }
    }
}
