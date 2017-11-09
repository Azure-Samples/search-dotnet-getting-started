using Newtonsoft.Json;
using System;

namespace DotNetHowToSecurityTrimming
{
    public class BatchResponse
    {
        public string Id { get; set; }

        public BatchResponseBody Body { get; set; }
    }

    public class BatchResponseBody
    {
        public BatchResponseBodyValue[] Value { get; set; }
    }

    public class BatchResponseBodyValue
    {
        [JsonProperty("@odata.type")]
        public string Type { get; set; }

        public string Id { get; set; }

        public string DisplayName { get; set; }
    }

    public class BatchResult
    {
        public BatchResponse[] Responses { get; set; }
    }
}
