using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using log4net;

namespace ADCleanse
{
    public class ActiveDirectoryQuery
    {
        private readonly QueryConfiguration _configuration;
        static ILog Log { get; set; }

        public ActiveDirectoryQuery(QueryConfiguration configuration)
        {
            _configuration = configuration;
            Log = LogManager.GetLogger(typeof(ActiveDirectoryQuery));
        }

        public void Execute()
        {
            using (var root = GetRootDirectoryEntry())
            {
                Log.InfoFormat("Executing query {0} and writing results to {1}", 
                    _configuration.Filter,
                    _configuration.OutputFilePath);

                var propertiesToLoad = _configuration.Properties.ToArray();
                var recordCount = 0;

                using (var searcher = new DirectorySearcher(root, _configuration.Filter, propertiesToLoad))
                using (var writer = new StreamWriter(_configuration.OutputFilePath, false))
                {
                    searcher.PageSize = 1001;
                    writer.WriteLine(string.Join("\t", propertiesToLoad));
                    SearchResultCollection resultCollection = searcher.FindAll();
                    var fieldValues = new List<string>(propertiesToLoad.Length);

                    foreach (SearchResult result in resultCollection)
                    {
                        fieldValues.AddRange(propertiesToLoad
                            .Select(propertyName => GetPropertyValue(result, propertyName)));
                        writer.WriteLine(string.Join("\t", fieldValues));
                        fieldValues.Clear();
                        if ((recordCount % 100) == 0)
                            Log.InfoFormat("{0} records found", ++recordCount);
                    }
                }
                Log.InfoFormat("{0} records found", recordCount);

                if (_configuration.AutoOpen)
                    Process.Start(_configuration.OutputFilePath);
            }
        }

        private DirectoryEntry GetRootDirectoryEntry()
        {
            Log.InfoFormat("Creating connection to {0}", _configuration.LdapRoot);
            if (string.IsNullOrEmpty(_configuration.Username))
                return new DirectoryEntry(_configuration.LdapRoot);

            return new DirectoryEntry(_configuration.LdapRoot, _configuration.Username, _configuration.Password);
        }


        private static string GetPropertyValue(SearchResult result, string name)
        {
            ResultPropertyValueCollection values = result.Properties[name];
            return (values != null && values.Count > 0) ? values[0].ToString() : string.Empty;
        }
    }
}