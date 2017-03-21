using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaSplitter
{
    public static class Log
    {
        private static void WriteDate()
        {
            Console.Write("{0}: ", DateTime.Now.ToString());
        }

        public static void ClearCurrentConsoleLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write(new String(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        public static void WriteLine(string str, params object[] args)
        {
            WriteDate();
            Console.Write(str, args);
            Console.Write(Environment.NewLine);
        }

        public static void WriteHeader(string str, params object[] args)
        {
            WriteLine("======================================================================");
            WriteLine(str, args);
            WriteLine("======================================================================");
        }

        public static void WriteSubHeader(string str, params object[] args)
        {
            WriteLine("-----------------------------------");
            WriteLine(str, args);
            WriteLine("-----------------------------------");
        }

        public static void WriteLine(string str, ConsoleColor color, params object[] args)
        {
            // Write the date as gray.
            Console.ForegroundColor = ConsoleColor.Gray;
            WriteDate();
            Console.ResetColor();

            // Write the data as the color.
            Console.ForegroundColor = color;
            Console.Write(str, args);
            Console.ResetColor();

            // Place a new line to ensure only 1 item on each line.
            Console.Write(Environment.NewLine);
        }
    }
}
