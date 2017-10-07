using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BITS
{
    class IPEndPointConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>((string)reader.Value);
            return new IPEndPoint(IPAddress.Parse(dict["IP"]), Int16.Parse(dict["Port"]));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var ipendpoint = (IPEndPoint)value;
            var dict = new Dictionary<string, string>
            {
                ["IP"] = ipendpoint.Address.ToString(),
                ["Port"] = ipendpoint.Port.ToString()
            };
            writer.WriteValue(JsonConvert.SerializeObject(dict));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPEndPoint);
        }
    }
}
