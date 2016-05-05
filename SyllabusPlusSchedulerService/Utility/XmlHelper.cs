using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SyllabusPlusSchedulerService.Utility
{
    internal class XmlHelper<TType>
    {
        private XmlSerializer xmlSerializer;

        public XmlHelper()
        {
            this.xmlSerializer = new XmlSerializer(typeof(TType));
        }

        /// <summary>
        /// Serializes the object to an XML formatted string
        /// </summary>
        /// <param name="obj">Object to serialize to XML</param>
        /// <returns>XML formatted string of the object</returns>
        public string SerializeXMLToString(TType obj)
        {
            using (StringWriter strWriter = new StringWriter())
            {
                this.xmlSerializer.Serialize(strWriter, obj);

                return strWriter.ToString();
            }
        }
    }
}
