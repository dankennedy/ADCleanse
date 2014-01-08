using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text.RegularExpressions;
using log4net;

namespace ADCleanse
{
    public class ActiveDirectoryCleaner
    {
        private readonly CleanseConfiguration _configuration;
        private readonly string[] _defaultPropeties =
        {
            "sAMAccountName"
        };
        private readonly IDictionary<string, List<string>> _unMatchedPropertyValues = 
            new Dictionary<string, List<string>>();

        static ILog Log { get; set; }

        public ActiveDirectoryCleaner(CleanseConfiguration configuration)
        {
            _configuration = configuration;
            Log = LogManager.GetLogger(typeof(ActiveDirectoryCleaner));
        }

        public void Clean()
        {
            _unMatchedPropertyValues.Clear();
            using (var root = GetRootDirectoryEntry())
            {
                var propertiesToLoad = _defaultPropeties.Concat(_configuration.Properties.Select(x => x.Name))
                    .Distinct()
                    .ToArray();

                Log.InfoFormat("Retrieving items using filter {0}", _configuration.Filter);
                using (var searcher = new DirectorySearcher(root, _configuration.Filter, propertiesToLoad))
                {
                    SearchResultCollection resultCollection = searcher.FindAll();
                    foreach (SearchResult result in resultCollection)
                    {
                        Log.InfoFormat("Found {0}", result.Properties["sAMAccountName"][0]);
                        CheckAndCleanResult(result, NotMatched);
                    }
                }
            }
            
            Log.Info("Unmatched property values");
            foreach (var unMatchedPropertyValue in _unMatchedPropertyValues)
            {
                Log.Info(unMatchedPropertyValue.Key);
                foreach (var propertyValue in unMatchedPropertyValue.Value)
                    Log.Info("\t" + propertyValue);
            }
        }

        private DirectoryEntry GetRootDirectoryEntry()
        {
            Log.InfoFormat("Creating connection to {0}", _configuration.LdapRoot);
            if (string.IsNullOrEmpty(_configuration.Username))
                return new DirectoryEntry(_configuration.LdapRoot);
            
            return new DirectoryEntry(_configuration.LdapRoot, _configuration.Username, _configuration.Password);
        }

        private void NotMatched(string propertyName, string value)
        {
            Log.DebugFormat("Adding {0}={1} to unmatched list", propertyName, value);

            if (!_unMatchedPropertyValues.ContainsKey(propertyName))
                _unMatchedPropertyValues.Add(propertyName, new List<string> {value});
            else if (!_unMatchedPropertyValues[propertyName].Contains(value))
                _unMatchedPropertyValues[propertyName].Add(value);
        }

        private void CheckAndCleanResult(SearchResult result, Action<string, string> notMatched)
        {
            DirectoryEntry entry = null;
            foreach (var property in _configuration.Properties)
            {
                Log.DebugFormat("Checking {0}", property.Name);
                var stringValue = GetPropertyValue(result, property.Name);
                var matched = false;

                foreach (var match in property.Matches)
                {
                    if (!Regex.IsMatch(stringValue, match.Pattern)) continue;

                    Log.InfoFormat("Matched {0}. Updating {1} from {2} to {3}",
                        match.Pattern,
                        property.Name,
                        stringValue,
                        match.Value);

                    if (entry == null)
                        entry = result.GetDirectoryEntry();

                    entry.Properties[property.Name].Value = match.Value;
                    matched = true;
                    break;
                }

                if (matched) continue;

                Log.DebugFormat("No match found for {0}", property.Name);
                notMatched(property.Name, stringValue);
            }

            if (entry != null)
                entry.CommitChanges();
        }

        private static string GetPropertyValue(SearchResult result, string name)
        {
            ResultPropertyValueCollection values = result.Properties[name];
            var stringValue = string.Empty;
            if (values == null || values.Count == 0)
                Log.Debug("Value is null");
            else
            {
                stringValue = values[0].ToString();
                Log.DebugFormat("Value is {0}", stringValue);
            }
            return stringValue;
        }
    }
}