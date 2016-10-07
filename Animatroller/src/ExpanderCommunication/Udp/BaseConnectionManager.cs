using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Animatroller.ExpanderCommunication
{
    public abstract class BaseConnectionManager
    {
        private JsonSerializer payloadSerializer;

        public BaseConnectionManager()
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter()
                },
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
            };

            this.payloadSerializer = JsonSerializer.Create(settings);
        }

        internal string SerializeInternalMessage(Model.BaseMessage message)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter()
                }
            };

            var data = JsonConvert.SerializeObject(message, settings);

            return data;
        }

        internal byte[] SerializePayload(object payloadObject)
        {
            using (var ms = new MemoryStream())
            using (var tw = new StreamWriter(ms))
            {
                this.payloadSerializer.Serialize(tw, payloadObject);

                tw.Flush();

                ms.Position = 0;

                return ms.ToArray();
            }
        }

        internal object DeserializePayload(byte[] payload)
        {
            using (var ms = new MemoryStream(payload))
            using (var sr = new StreamReader(ms))
            using (var jr = new JsonTextReader(sr))
            {
                return this.payloadSerializer.Deserialize(jr);
            }
        }

        internal Model.BaseMessage DeserializeInternalMessage(string data)
        {
            var jObject = JObject.Parse(data);
            var msgTypeNode = jObject.SelectToken("Type");
            if (msgTypeNode == null)
                throw new ArgumentException("Missing message type");

            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter()
                }
            };

            var jsonSerializer = JsonSerializer.Create(settings);

            var msgType = msgTypeNode.ToObject<Model.BaseMessage.MessageTypes>(jsonSerializer);
            switch (msgType)
            {
                case Model.BaseMessage.MessageTypes.Connect:
                    return jObject.ToObject<Model.ConnectMessage>(jsonSerializer);

                case Model.BaseMessage.MessageTypes.Alive:
                    return jObject.ToObject<Model.AliveMessage>(jsonSerializer);

                case Model.BaseMessage.MessageTypes.Payload:
                    return jObject.ToObject<Model.PayloadMessage>(jsonSerializer);

                default:
                    throw new ArgumentException("Unknown message type " + msgType.ToString());
            }
        }
    }
}

