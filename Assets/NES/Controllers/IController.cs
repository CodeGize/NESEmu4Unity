//using System.Windows.Forms;

namespace dotNES.Controllers
{
    public interface IController
    {
        void Strobe(bool on);

        int ReadState();

        //void PressKey(KeyEventArgs e);

        //void ReleaseKey(KeyEventArgs e);
    }
}
