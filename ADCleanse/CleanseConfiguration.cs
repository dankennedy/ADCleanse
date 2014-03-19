using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        public virtual int PageSize { get; set; }

        [XmlArray]
        [XmlArrayItem("Property")]
        public virtual List<Property> Properties { get; set; }

        private IList<IsoCountry> _countries;
        public virtual IEnumerable<IsoCountry> IsoCountries
        {
            get
            {
                if (_countries == null)
                {
                    _countries = new List<IsoCountry>();
                    using (var stream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("ADCleanse.Resources.IsoCountries.txt"))
                    using (var reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var parts = line.Split('\t');
                            _countries.Add(new IsoCountry
                            {
                                A2 = parts[0],
                                A3 = parts[1],
                                Number = parts[2],
                                Name = parts[3]
                            });
                        }
                    }
                }
                return _countries;
            }
        }

        private IList<CountryCorrection> _countryCorrections;

        public virtual IEnumerable<CountryCorrection> CountryCorrections
        {
            get
            {
                if (_countryCorrections == null)
                {
                    _countryCorrections = new List<CountryCorrection>();
                    using (var stream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("ADCleanse.Resources.CountryCorrections.txt"))
                    using (var reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var parts = line.Split('\t');
                            _countryCorrections.Add(new CountryCorrection
                            {
                                Ou1 = parts[0],
                                Ou2 = parts[1],
                                CountryName = parts[2],
                                TwoCharCode = parts[3],
                                CountryCode = int.Parse(parts[4])
                            });
                        }
                    }
                }
                return _countryCorrections;
            }
        }
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
        public virtual string Function { get; set; }
    }
}