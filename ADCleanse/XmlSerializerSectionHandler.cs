using System;
using System.Configuration;
using System.Xml;
using System.Xml.Serialization;

namespace ADCleanse
{
    public class XmlSerializerSectionHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            // get the name of the type from the type= attribute on the root node
            var navigator = section.CreateNavigator();
            var typeName = navigator.Evaluate("string(@type)").ToString();
            if (string.IsNullOrEmpty(typeName))
                throw new ConfigurationErrorsException(string.Format("The type attribute is not present on the root " +
                                                                     "node of the <{0}> configuration section ",
                    section.Name),
                    section);

            // make sure this string evaluates to a valid type
            var type = Type.GetType(typeName);
            if (type == null)
                throw new ConfigurationErrorsException(string.Format("The type attribute '{0}' specified on the root " +
                                                                     "node of the <{0}> configuration section, '{1}', is not a valid type.",
                    section.Name, typeName), section);
            var serializer = new XmlSerializer(type);

            // attempt to deserialize an object of this type from the provided XML section
            var nodeReader = new XmlNodeReader(section);
            try
            {
                return serializer.Deserialize(nodeReader);
            }
            catch (Exception ex)
            {
                var exceptionMessage = ex.Message;
                Exception innerException;
                while ((innerException = ex.InnerException) != null)
                    exceptionMessage += " " + innerException.Message;

                throw new ConfigurationErrorsException(
                    string.Format("Unable to deserialize an object of type '{0}' from the <{1}> " +
                                  "configuration section. " + exceptionMessage, typeName, section.Name), ex, section);
            }
        }
    }
}