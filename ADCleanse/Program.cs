using System;
using System.Collections.Generic;
using System.Configuration;
using log4net;

namespace ADCleanse
{
    public class Program
    {
        public static CommandLineArguments Options { get; private set; }

        static ILog Log { get; set; }

        public static int Main(string[] args)
        {
            try
            {
                log4net.Config.XmlConfigurator.Configure();
                Log = LogManager.GetLogger(typeof(Program));
                Options = new CommandLineArguments(args);
                if (Options.HelpRequested)
                {
                    Log.Info(Options.UsageMessage);
                    return 0;
                }

                var validationErrors = new List<string>();
                if (!Options.Validate(validationErrors))
                {
                    foreach (var error in validationErrors)
                        Log.ErrorFormat("Error. {0}", error);
                    return -1;
                }

                switch (Options.Action)
                {
                    case ActionEnum.Query:
                        var query = new ActiveDirectoryQuery((QueryConfiguration)
                            ConfigurationManager.GetSection(typeof(QueryConfiguration).Name));
                        query.Execute();
                        break;
                    case ActionEnum.Clean:
                        var cleaner = new ActiveDirectoryCleaner((CleanseConfiguration)
                            ConfigurationManager.GetSection(typeof(CleanseConfiguration).Name));
                        cleaner.Clean();
                        break;
                    case ActionEnum.PrepareBeta:
                        var prepareBeta = new ActiveDirectoryPrepareBeta((PrepareBetaConfiguration)
                            ConfigurationManager.GetSection(typeof(PrepareBetaConfiguration).Name));
                        prepareBeta.Execute();
                        break;
                    case ActionEnum.PrepareChina:
                        var prepareChina = new ActiveDirectoryPrepareChina((PrepareChinaConfiguration)
                            ConfigurationManager.GetSection(typeof(PrepareChinaConfiguration).Name));
                        prepareChina.Execute();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Log.Info("Finished");

                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return -1;
            }
        }
    }
}
