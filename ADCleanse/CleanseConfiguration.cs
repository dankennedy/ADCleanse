using System.Collections.Generic;
using System.Xml.Serialization;

namespace ADCleanse
{
    public class CleanseConfiguration
    {
        public virtual string Filter { get; set; }
        public virtual string LdapRoot { get; set; }
        public virtual string Username { get; set; }
        public virtual string Password { get; set; }
        public virtual bool DryRun { get; set; }

        [XmlArray]
        [XmlArrayItem("Property")]
        public virtual List<Property> Properties { get; set; }
    }

    public class Property
    {
        public virtual string Name { get; set; }

        [XmlArray]
        [XmlArrayItem("Match")]
        public virtual List<Match> Matches { get; set; }
    }

    public class Match
    {
        public virtual string Pattern { get; set; }
        public virtual string Value { get; set; }
    }
}