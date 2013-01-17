using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using NLog;
using LMS = Animatroller.Framework.Import.Schemas.LightORama.LMS;

namespace Animatroller.Framework.Utility
{
    public class LorImport
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        // Light-O-Rama Musical Sequence
        public LorImport ImportLMSFile(string filename)
        {
            LMS.sequence sequence;

            var deserializer = new XmlSerializer(typeof(LMS.sequence));

            using (TextReader textReader = new StreamReader(filename))
            {
                sequence = (LMS.sequence)deserializer.Deserialize(textReader);
            }

            return this;
        }
    }
}
