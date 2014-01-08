using System;
using System.Collections;
using System.Collections.Generic;

namespace ADCleanse
{
	public class CommandLineArguments : CommandLineArgumentsBase
    {
        private const string ARG_HELP = "?";

        private static readonly string[] AllArgs = {
                                                       ARG_HELP
                                                   };
        public CommandLineArguments(string[] args) : base(args)
        {
            Array.Sort(AllArgs);
        }
        public CommandLineArguments(string commandLine) : base(commandLine)
        {
            Array.Sort(AllArgs);
        }

        public bool HelpRequested
        {
            get { return Parameters.ContainsKey(ARG_HELP); }
        }

        public override string UsageMessage
        {
            get
            {
                return string.Concat("ADCleanse - Matches and updates/cleans attributes of AD items\r\n\r\n",
									 "Usage: ADCleanse.exe\r\n",
                                     "             [-?]\r\n",
                                     "\r\n",
                                     "Examples:\r\n",
                                     "ADCleanse.exe\r\n");
            }
        }

        public override bool Validate(IList validationErrors)
        {
            if (validationErrors == null)
                validationErrors = new List<string>();

            var errorCountOnEntry = validationErrors.Count;

            foreach (string param in Parameters.Keys)
            {
                if (Array.BinarySearch(AllArgs, param) < 0)
                    validationErrors.Add(String.Format("Invalid argument '{0}'", param));
            }

            return validationErrors.Count == errorCountOnEntry;
        }
    }
}