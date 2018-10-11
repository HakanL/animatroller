using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Animatroller.AdminMessage
{
    public class SceneComponent
    {
        public string Id { get; set; }

        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ComponentType Type { get; set; }
    }
}
