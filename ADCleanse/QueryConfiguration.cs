using System.Collections.Generic;
using System.Xml.Serialization;

namespace ADCleanse
{
    public class QueryConfiguration
    {
        public virtual string Filter { get; set; }
        public virtual string LdapRoot { get; set; }
        public virtual string Username { get; set; }
        public virtual string Password { get; set; }
        public virtual string OutputFilePath { get; set; }
        public virtual bool AutoOpen { get; set; }

        [XmlArray]
        [XmlArrayItem("Property")]
        public virtual List<string> Properties { get; set; }

    }
}