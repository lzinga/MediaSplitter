using MediaSplitter.Common;
using MediaSplitter.Services;
using Ninject;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaSplitter
{
    public class ProductionBindings : NinjectModule
    {
        public override void Load()
        {
            Bind<ILogService>().To<LogService>();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            List<string> argsList = args.ToList();
            argsList.Add("/Extensions=.m4v");
            argsList.Add("/StartRange=00:11:00");
            argsList.Add("/EndRange=00:12:00");


            StandardKernel kernal = new StandardKernel();
            kernal.Load<ProductionBindings>();
            ILogService log = kernal.Get<ILogService>();
            Setup setup = new Setup(new Arguments(argsList.ToArray()), log);


            log.WriteHeader($"Exit Code: {setup.Execute()} ({(int)setup.Execute()})");


            Console.ReadLine();
        }
    }
}
