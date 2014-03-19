using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using log4net;

namespace ADCleanse
{
    public class ActiveDirectoryPrepareBeta
    {
        private readonly PrepareBetaConfiguration _configuration;
        static ILog Log { get; set; }

        public ActiveDirectoryPrepareBeta(PrepareBetaConfiguration configuration)
        {
            _configuration = configuration;
            Log = LogManager.GetLogger(typeof(ActiveDirectoryPrepareBeta));
        }

        public void Execute()
        {
            Log.InfoFormat("Opening input file path {0}", _configuration.InputFilePath);
            var emailAddresses = File.ReadAllLines(_configuration.InputFilePath)
                .Select(x => x.ToLowerInvariant())
                .Distinct()
                .ToList();

            Log.InfoFormat("{0} unique entries", emailAddresses.Count);

            if (File.Exists(_configuration.OutputFilePath))
                File.Delete(_configuration.OutputFilePath);

            var betaGroup = GetBetaGroup();

            var groupMembers = betaGroup.Properties["member"] == null || betaGroup.Properties["member"].Value == null 
                ? new List<string>() 
                : ((object[])betaGroup.Properties["member"].Value).Select(x => x.ToString()).ToList();

            using (var outputFile = File.CreateText(_configuration.OutputFilePath))
            using (var root = GetRootDirectoryEntry())
            {
                foreach (var emailAddress in emailAddresses)
                {
                    using (var searcher = new DirectorySearcher(root, 
                        string.Format("(&(objectCategory=person)(objectClass=user)(mail={0}))", 
                        emailAddress),
                        new []{"sAMAccountName","distinguishedName"}))
                    {
                        var result = searcher.FindOne();
                        if (result == null)
                        {
                            Log.WarnFormat("Failed to find account name for email address {0}", 
                                emailAddress);
                            continue;
                        }

                        outputFile.WriteLine(GetPropertyValue(result, "sAMAccountName") + ",1");

                        var userDistName = GetPropertyValue(result, "distinguishedName");

                        if (!groupMembers.Contains(userDistName))
                        {
                            Log.InfoFormat("Adding {0} to beta group", userDistName);
                            try
                            {
                                betaGroup = GetBetaGroup();
                                betaGroup.Properties["member"].Add(userDistName);
                                betaGroup.CommitChanges();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            betaGroup.Dispose();
                        }
                        else
                            Log.InfoFormat("Skipping {0}. Already in group", userDistName);
                    }    
                }
            }
            betaGroup.CommitChanges();
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