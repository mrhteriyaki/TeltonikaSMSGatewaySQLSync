using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeltonikaService;

namespace TestConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TeltonikaFunctions.LoadConfig();
            TeltonikaFunctions.GetMessageList();

        }
    }
}
