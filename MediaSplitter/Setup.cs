using MediaSplitter.Common;
using MediaSplitter.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaSplitter
{
    public class Setup
    {
        private ILogService Log;
        private Arguments Arguments;

        public Setup(Arguments args, ILogService log)
        {
            this.Log = log;
            Arguments = args;

            Log.WriteHeader($"Media Splitter - {System.Reflection.Assembly.GetEntryAssembly().GetName().Version}");
            foreach(string val in args.Get())
            {
                Log.WriteLine(val);
            }

            if (Arguments.Debug)
            {
                Log.WriteDebug();
            }

        }

        //public List<string> GetMedia(string path)
        //{
        //    var attr = File.GetAttributes(path);

        //    if (attr.HasFlag(FileAttributes.Directory))
        //    {
        //        foreach (string file in Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly).Where(i => this.Extensions.Contains(Path.GetExtension(i))))
        //        {
        //            string fileName = Path.GetFileNameWithoutExtension(file);

        //            if (Regex.IsMatch(fileName, RegexMultiEpisodeFile))
        //            {
        //                yield return new FileInfo(file);
        //            }
        //        }
        //    }




        //}


        public ExitCode Execute()
        {



            return ExitCode.Success;
        }



    }
}
