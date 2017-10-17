using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetHowToSecurityTrimming
{
    class Result
    {
        public Response[] Responses { get; set; }
    }

    class Response
    {
        public string Id { get; set; }

        public ResponseBody Body { get; set; }
    }

    class ResponseBody
    {
        public ResponseBodyValue[] Value { get; set; }
    }

    class ResponseBodyValue
    {
        [JsonProperty("@odata.type")]
        public string Type { get; set; }

        public string Id { get; set; }

        public string DisplayName { get; set; }
    }

}
