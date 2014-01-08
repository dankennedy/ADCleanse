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

                var config = (CleanseConfiguration)ConfigurationManager.GetSection(typeof(CleanseConfiguration).Name);
                var cleaner = new ActiveDirectoryCleaner(config);
                cleaner.Clean();

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
