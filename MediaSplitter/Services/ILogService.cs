using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaSplitter.Services
{
    public interface ILogService
    {
        void WriteDebug();
        void WriteLine(string str);
        void WriteLine(string str, params object[] args);
        void WriteHeader(string str);
        void WriteHeader(string str, params object[] args);
    }
}
