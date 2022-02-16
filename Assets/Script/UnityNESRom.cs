using dotNES;

namespace NESGame
{
    public class UnityNESRom : IRom
    {
        public string Path { get; }

        public byte[] Data { get; }

        public UnityNESRom(string path,byte[] data)
        {
            Path = path;
            Data = data;
        }
    }
}
