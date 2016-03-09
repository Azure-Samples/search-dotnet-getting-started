using System;
using Microsoft.Azure.Search.Models;
using Microsoft.Spatial;
using Newtonsoft.Json;

namespace AzureSearch.SDKHowTo
{
    [SerializePropertyNamesAsCamelCase]
    public partial class Hotel
    {
        public string HotelId { get; set; }

        public double? BaseRate { get; set; }

        public string Description { get; set; }

        [JsonProperty("description_fr")]
        public string DescriptionFr { get; set; }

        public string HotelName { get; set; }

        public string Category { get; set; }

        public string[] Tags { get; set; }

        public bool? ParkingIncluded { get; set; }

        public bool? SmokingAllowed { get; set; }

        public DateTimeOffset? LastRenovationDate { get; set; }

        public int? Rating { get; set; }

        public GeographyPoint Location { get; set; }

        // ToString() method omitted for brevity...
    }
}
