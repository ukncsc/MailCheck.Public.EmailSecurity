using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MailCheck.EmailSecurity.Entity
{
    public static class SerialisationConfig
    {
        public static JsonSerializerSettings Settings => new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            // Converters = new List<JsonConverter> { new StringEnumConverter(), new TagConverter(), new KeyConverter() }
        };
    }
}
