using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7Core
{
    // This is thread for test purposes
    internal class S7CoreThread1
    {
        // Constructor
        public S7CoreThread1()
        {

        }

        // Infinite task
        public void InfiniteTask(S7CoreSupervisor supervisor)
        {
            while(true)
            {
                Thread.Sleep(200);
                supervisor.ToLogFile("Bla 1");
            }
        }

    }
}
