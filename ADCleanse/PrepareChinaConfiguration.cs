using System.Collections.Generic;
using System.Xml.Serialization;

namespace ADCleanse
{
    public class PrepareChinaConfiguration
    {
        public virtual string LdapRoot { get; set; }
        public virtual string Username { get; set; }
        public virtual string Password { get; set; }
        public virtual string OutputFilePath { get; set; }
        public virtual string BetaGroupName { get; set; }

        [XmlArray]
        [XmlArrayItem("OU")]
        public virtual List<string> OUs { get; set; }
    }
}