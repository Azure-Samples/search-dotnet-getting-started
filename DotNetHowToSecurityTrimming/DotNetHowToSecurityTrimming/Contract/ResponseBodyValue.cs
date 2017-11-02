using System;
using Newtonsoft.Json;

namespace DotNetHowToSecurityTrimming
{
    public class ResponseBodyValue
    {
        [JsonProperty("@odata.type")]
        public string Type { get; set; }

        public string Id { get; set; }

        public string DisplayName { get; set; }
    }

}
