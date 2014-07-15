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

        static List<Thread> Threads = new List<Thread>();

        public static void StopThread(ThreadStart method)
        {
            int i;
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
        
        public static void ThreadRunner(ThreadStart method)
        {
            var th = new Thread(method)
            {
                IsBackground = true
            };

            th.Start();
            
            if (th.IsAlive)
            {
                Threads.Add(th);
                Methods.Add(method);
            }
        }
    }
}
