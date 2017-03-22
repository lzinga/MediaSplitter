using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaSplitter.Services
{
    public class LogService : ILogService
    {
        public void WriteDebug()
        {
            WriteHeader("Press any key to continue...");
            Console.ReadKey();
        }

        public void WriteHeader(string str)
        {
            WriteHeader(str, null);
        }

        public void WriteHeader(string str, params object[] args)
        {
            WriteLine("======================================================================");
            WriteLine(str, args);
            WriteLine("======================================================================");
        }

        public void WriteLine(string str)
        {
            WriteLine(str, null);
        }

        public void WriteLine(string str, params object[] args)
        {
            Console.WriteLine($"{DateTime.Now}: {str}", args);
        }
    }
}
