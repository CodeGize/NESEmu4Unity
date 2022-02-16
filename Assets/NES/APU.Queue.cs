using System;

namespace dotNES
{
    partial class APU
    {
        private struct QueueData
        {
            public long time;
            public uint addr;
            public byte data;
            public byte reserved;
        }

        const int QUEUE_LENGTH = 8192;
        private struct Queue
        {
            public int rdptr;
            public int wrptr;
            public QueueData[] data;

            public Queue(int len)
            {
                this.rdptr = 0;
                this.wrptr = 0;
                this.data = new QueueData[len];
            }
        }

        private Queue queue = new Queue(QUEUE_LENGTH);

        private void SetQueue(long writetime, uint addr, byte data)
        {
            var qdata = new QueueData { time = writetime, addr = addr, data = data };
            queue.data[queue.wrptr] = qdata;
            queue.wrptr++;
            queue.wrptr &= QUEUE_LENGTH - 1;
            if (queue.wrptr == queue.rdptr)
            {
                //DEBUGOUT("queue overflow.\n");
            }
        }

        private bool GetQueue(float writetime, out QueueData ret)
        {
            if (queue.wrptr != queue.rdptr)
            {
                if (queue.data[queue.rdptr].time <= writetime)
                {
                    ret = queue.data[queue.rdptr];
                    queue.rdptr++;
                    queue.rdptr &= QUEUE_LENGTH - 1;
                    return true;
                }
            }
            ret = new QueueData();
            return false;
        }

        private void QueueClear()
        {
            queue = new Queue(QUEUE_LENGTH);
        }

        private void QueueFlush()
        {
            while (queue.wrptr != queue.rdptr)
            {
                WriteProcess(queue.data[queue.rdptr].addr, queue.data[queue.rdptr].data);
                queue.rdptr++;
                queue.rdptr &= QUEUE_LENGTH - 1;
            }
        }
    }
}
