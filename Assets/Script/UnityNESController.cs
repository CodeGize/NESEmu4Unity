using System.Collections.Generic;
using dotNES;
using dotNES.Controllers;
using UnityEngine;

namespace NESGame
{
    public class UnityNESController : MonoBehaviour, IController
    {
        public KeyCode A = KeyCode.J;
        public KeyCode B = KeyCode.K;
        public KeyCode As = KeyCode.U;
        public KeyCode Bs = KeyCode.I;
        public KeyCode Select = KeyCode.V;
        public KeyCode Start = KeyCode.B;
        public KeyCode Up = KeyCode.W;
        public KeyCode Down = KeyCode.S;
        public KeyCode Left = KeyCode.A;
        public KeyCode Right = KeyCode.D;

        private int data;
        private int serialData;
        private bool strobing;

        
        protected void Awake()
        {
            m_keyMapping = new Dictionary<KeyCode, int>
            {
                { A, 7},
                { B, 6},
                { Select, 5},
                { Start, 4},
                { Up, 3},
                { Down, 2},
                { Left, 1},
                { Right, 0},
            };
        }

        private Dictionary<KeyCode, int> m_keyMapping;

        public int ReadState()
        {
            int ret = ((serialData & 0x80) > 0).AsByte();
            if (!strobing)
            {
                serialData <<= 1;
                serialData &= 0xFF;
            }
            return ret;
        }

        public void Strobe(bool on)
        {
            serialData = data;
            strobing = on;
        }

        public void Update()
        {
            foreach (var kp in m_keyMapping)
            {
                if (Input.GetKey(kp.Key))
                {
                    data |= 1 << m_keyMapping[kp.Key];
                }
                if (Input.GetKeyUp(kp.Key))
                {
                    data &= ~(1 << m_keyMapping[kp.Key]);
                }
            }
        }
    }
}
