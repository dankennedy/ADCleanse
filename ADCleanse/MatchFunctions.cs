using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text.RegularExpressions;
using log4net;

namespace ADCleanse
{
    public sealed class MatchFunctions
    {
        static readonly MatchFunctions Singleton = new MatchFunctions();
        static ILog Log { get; set; }

        static MatchFunctions()
        {
            Log = LogManager.GetLogger(typeof(MatchFunctions));
        }

        MatchFunctions() {  }

        static readonly Regex OuRegex = new Regex(@"OU=((?:\\.|[^,\\]+)+)", RegexOptions.Compiled);

        private IDictionary<string, Func<Property, string, DirectoryEntry, MatchFunctionResult>> _functions;
        public IDictionary<string, Func<Property, string, DirectoryEntry, MatchFunctionResult>> Functions
        {
            get
            {
                return _functions ??
                       (_functions = new Dictionary<string, Func<Property, string, DirectoryEntry, MatchFunctionResult>>
                       {
                           {"SetCountryNameFromCode", Instance.SetCountryNameFromCode},
                           {"SetCountryPropertiesFromOu", Instance.SetCountryPropertiesFromOu}
                       });
            }
        }

        public static MatchFunctions Instance { get { return Singleton; } }

        public CleanseConfiguration Configuration { get; set; }

        MatchFunctionResult SetCountryNameFromCode(Property property, string stringValue, DirectoryEntry entry)
        {
            var existingCountryName = entry.Properties["co"].Value as string;
            if (!string.IsNullOrEmpty(existingCountryName))
            {
                Log.InfoFormat("Skipping update. Entry already has country name {0}", 
                    existingCountryName);
                return new MatchFunctionResult { Success = false };
            }

            Log.InfoFormat("Setting country name from code {0} for {1}", 
                stringValue, entry.Name);

            var isoCountry =
                Configuration.IsoCountries.FirstOrDefault(
                    x => string.Equals(x.A2, stringValue, StringComparison.OrdinalIgnoreCase));

            if (isoCountry == null)
            {
                Log.ErrorFormat("Unable to find IsoCountry for code {0}",
                    stringValue);
                return new MatchFunctionResult { Success = false };
            }

            Log.InfoFormat("Setting codes from IsoCountry {0},{1},{2}",
                isoCountry.A2,
                isoCountry.Number,
                isoCountry.Name);

            entry.Properties["co"].Value = isoCountry.Name;

            return new MatchFunctionResult
            {
                Success = true, 
                Value = stringValue.ToUpperInvariant()
            };
        }

        private MatchFunctionResult SetCountryPropertiesFromOu(Property property, 
            string stringValue, DirectoryEntry entry)
        {
            var distName = entry.Properties["distinguishedName"][0].ToString();
            Log.Info(distName);
            var matches = OuRegex.Matches(distName);
            var ouList = new List<string>(matches.Count);
            for (var i = matches.Count - 1; i >= 0; i--)
                ouList.Add(matches[i].Groups[matches[i].Groups.Count-1].Value);

            var countryCorrection = Configuration.CountryCorrections.FirstOrDefault(x =>
                ouList.Contains(x.Ou1) && ouList.Contains(x.Ou2));

            if (countryCorrection == null)
                return new MatchFunctionResult {Success = false};

            var isoCountry =
                Configuration.IsoCountries.FirstOrDefault(
                    x => string.Equals(x.Name, countryCorrection.CountryName, 
                        StringComparison.OrdinalIgnoreCase));

            if (isoCountry == null)
            {
                Log.ErrorFormat("Unable to find IsoCountry for CountryCorrection. {0}", 
                    countryCorrection.CountryName);
                return new MatchFunctionResult {Success = false};
            }

            Log.InfoFormat("Setting codes from IsoCountry {0},{1},{2}", 
                isoCountry.A2,
                isoCountry.Number,
                isoCountry.Name);

            entry.Properties["countryCode"].Value = isoCountry.Number;
            entry.Properties["co"].Value = isoCountry.Name;

            return new MatchFunctionResult
            {
                Success = true, 
                Value = isoCountry.A2
            };
        }


    }
}