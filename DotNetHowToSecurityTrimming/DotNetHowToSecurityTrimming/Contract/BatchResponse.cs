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
        public string[] Value { get; set; }
    }

    public class BatchResult
    {
        public BatchResponse[] Responses { get; set; }
    }
}
