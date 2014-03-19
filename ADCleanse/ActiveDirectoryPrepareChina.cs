using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using log4net;

namespace ADCleanse
{
    public class ActiveDirectoryPrepareChina
    {
        private readonly PrepareChinaConfiguration _configuration;
        static ILog Log { get; set; }

        public ActiveDirectoryPrepareChina(PrepareChinaConfiguration configuration)
        {
            _configuration = configuration;
            Log = LogManager.GetLogger(typeof(ActiveDirectoryPrepareChina));
        }

        public void Execute()
        {
            if (File.Exists(_configuration.OutputFilePath))
                File.Delete(_configuration.OutputFilePath);

            var betaGroup = GetBetaGroup();

            var groupMembers = betaGroup.Properties["member"] == null || betaGroup.Properties["member"].Value == null
                ? new List<string>()
                : ((object[])betaGroup.Properties["member"].Value).Select(x => x.ToString()).ToList();

            using (var outputFile = File.CreateText(_configuration.OutputFilePath))
            using (var root = GetRootDirectoryEntry())
            {
                foreach (var ouName in _configuration.OUs)
                {
                    var ou = new DirectoryEntry(string.Format("{0}/ou=Users,ou={1},ou=Asia,{2}",
                        root.Path,
                        ouName,
                        root.Properties["distinguishedName"][0]),
                        _configuration.Username,
                        _configuration.Password);

                    try
                    {
                        using (var searcher = new DirectorySearcher(ou, 
                            "(&(objectCategory=person)(objectClass=user))",
                            new []{"sAMAccountName","mail","distinguishedName"}))
                        {
                            var results = searcher.FindAll();
                            foreach (SearchResult result in results)
                            {
                                outputFile.WriteLine(GetPropertyValue(result, "sAMAccountName") + ",1");
                                var userDistName = GetPropertyValue(result, "distinguishedName");
                                if (!groupMembers.Contains(userDistName))
                                {
                                    Log.InfoFormat("Adding {0} to beta group", userDistName);
                                    betaGroup.Properties["member"].Add(userDistName);
                                    betaGroup.CommitChanges();
                                }
                                else
                                    Log.InfoFormat("Skipping {0}. Already in group", userDistName);
                            }
                        }
                    }
                    catch (DirectoryServicesCOMException e)
                    {
                        Log.WarnFormat("Failed to retrieve users for the ou {0}. {1}", 
                            ouName,
                            e.Message);
                    }
                }
            }
        }

        private DirectoryEntry GetBetaGroup()
        {
            using (var root = GetRootDirectoryEntry())
            using (var searcher = new DirectorySearcher(root,
                string.Format("(&(objectClass=group)(CN={0}))", _configuration.BetaGroupName),
                new[] { "sAMAccountName" }))
            {
                var result = searcher.FindOne();
                if (result == null)
                    throw new ActiveDirectoryObjectNotFoundException(
                        string.Format("Unable to find beta group {0}", _configuration.BetaGroupName));

                return result.GetDirectoryEntry();
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