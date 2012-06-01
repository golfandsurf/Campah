using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CampahApp
{
    class ThreadManager
    {
        static List<ThreadStart> Methods = new List<ThreadStart>();
        public static void stopThread(ThreadStart method)
        {
            int i = 0;
            for (i = 0; i <= Methods.Count - 1; i++)
            {
                if (Methods[i].Method.Name == method.Method.Name)
                {
                    Threads[i].Abort();
                    Threads.RemoveAt(i);
                    Methods.RemoveAt(i);
                }
            }
        }

        static List<Thread> Threads = new List<Thread>();
        public static void threadRunner(ThreadStart method)
        {
            Thread TH = new Thread(method);
            TH.IsBackground = true;
            TH.Start();
            if (TH.IsAlive)
            {
                Threads.Add(TH);
                Methods.Add(method);
            }
        }

    }
}
