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
        private readonly IDictionary<string, Dictionary<string, int>> _unMatchedPropertyValues = 
            new Dictionary<string, Dictionary<string, int>>();

        static ILog Log { get; set; }

        public ActiveDirectoryCleaner(CleanseConfiguration configuration)
        {
            _configuration = configuration;
            Log = LogManager.GetLogger(typeof(ActiveDirectoryCleaner));
        }

        public void Clean()
        {
            if (_configuration.DryRun)
                Log.Info("Performing DryRun. All updates will be skipped");

            _unMatchedPropertyValues.Clear();
            MatchFunctions.Instance.Configuration = _configuration;
            var recordCount = 0;

            using (var root = GetRootDirectoryEntry())
            {
                var propertiesToLoad = _defaultPropeties.Concat(_configuration.Properties.Select(x => x.Name))
                    .Distinct()
                    .ToArray();

                Log.InfoFormat("Retrieving items using filter {0} using page size {1}", 
                    _configuration.Filter,
                    _configuration.PageSize);
                using (var searcher = new DirectorySearcher(root, _configuration.Filter, propertiesToLoad))
                {
                    searcher.PageSize = _configuration.PageSize;
                    SearchResultCollection resultCollection = searcher.FindAll();
                    foreach (SearchResult result in resultCollection)
                    {
                        try
                        {
                            Log.InfoFormat("Found {0}", result.Properties["sAMAccountName"][0]);
                            CheckAndCleanResult(result, NotMatched);
                            if ((++recordCount % 100) == 0)
                                Log.InfoFormat("{0} records processed", recordCount);    
                        }
                        catch (Exception e)
                        {
                            Log.Error("Failed to process SearchResult", e);
                        }
                    }

                    Log.InfoFormat("Total records processed: {0}", recordCount);
                }
            }
            
            Log.Info("Unmatched property values");
            foreach (var unMatchedPropertyValue in _unMatchedPropertyValues)
            {
                Log.Info(unMatchedPropertyValue.Key);
                foreach (var propertyValue in unMatchedPropertyValue.Value)
                    Log.InfoFormat("\t{0}\t{1}", propertyValue.Key, propertyValue.Value);
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
                _unMatchedPropertyValues.Add(propertyName, new Dictionary<string, int> {{value, 1}});
            else if (!_unMatchedPropertyValues[propertyName].ContainsKey(value))
                _unMatchedPropertyValues[propertyName].Add(value, 1);
            else _unMatchedPropertyValues[propertyName][value]++;
        }

        private void CheckAndCleanResult(SearchResult result, Action<string, string> notMatched)
        {
            DirectoryEntry entry = null;
            foreach (var property in _configuration.Properties)
            {
                var stringValue = entry == null ? 
                    GetPropertyValue(result, property.Name) : 
                    entry.Properties[property.Name].Value as string;

                stringValue = stringValue ?? string.Empty;

                Log.DebugFormat("Checking {0}={1}", property.Name, stringValue);

                var matched = false;

                foreach (var match in property.Matches)
                {
                    if (!Regex.IsMatch(stringValue, match.Pattern)) 
                        continue;

                    if (stringValue == match.Value)
                    {
                        Log.DebugFormat("Skipping match {0} as value is already {1}",
                            match.Pattern,
                            stringValue);

                        matched = true;
                        break;
                    }

                    if (!string.IsNullOrEmpty(match.Function))
                    {
                        Log.InfoFormat("Matched {0}. Updating {1} from {2} using MatchFunction {3}",
                            match.Pattern,
                            property.Name,
                            stringValue,
                            match.Function);

                        if (entry == null)
                            entry = result.GetDirectoryEntry();
                        
                        var functionResult = MatchFunctions.Instance.Functions[match.Function](property, stringValue, entry);
                        if (functionResult.Success)
                        {
                            Log.InfoFormat("Match function succeeded. Updating {0} to {1}",
                                property.Name,
                                functionResult.Value);

                            if (!_configuration.DryRun)
                                entry.Properties[property.Name].Value = functionResult.Value;

                            matched = true;
                            break;
                        }
                        
                        Log.Info("Match function failed");
                    }

                    if (!string.IsNullOrEmpty(match.Value))
                    {
                        Log.InfoFormat("Matched {0}. Updating {1} from {2} to {3}",
                            match.Pattern,
                            property.Name,
                            stringValue,
                            match.Value);

                        if (entry == null && !_configuration.DryRun)
                            entry = result.GetDirectoryEntry();

                        if (!_configuration.DryRun)
                            entry.Properties[property.Name].Value = match.Value;

                        matched = true;
                        break;
                    }

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
            return (values == null || values.Count <= 0) ? String.Empty : values[0] as string;
        }
    }
}