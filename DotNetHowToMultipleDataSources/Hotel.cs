namespace AzureSearch.SDKHowTo
{
    using System;
    using Microsoft.Azure.Search;
    using Microsoft.Azure.Search.Models;
    using Microsoft.Spatial;
    using Newtonsoft.Json;

    // The SerializePropertyNamesAsCamelCase attribute is defined in the Azure Search .NET SDK.
    // It ensures that Pascal-case property names in the model class are mapped to camel-case
    // field names in the index.
    [SerializePropertyNamesAsCamelCase]
    public partial class Hotel
    {
        [System.ComponentModel.DataAnnotations.Key]
        [IsFilterable]
        public string HotelId { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        public double? BaseRate { get; set; }

        [IsSearchable]
        public string Description { get; set; }

        [IsSearchable]
        [Analyzer(AnalyzerName.AsString.FrLucene)]
        [JsonProperty("description_fr")]
        public string DescriptionFr { get; set; }

        [IsSearchable, IsFilterable, IsSortable]
        public string HotelName { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        public string Category { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] Tags { get; set; }

        [IsFilterable, IsFacetable]
        public bool? ParkingIncluded { get; set; }

        [IsFilterable, IsFacetable]
        public bool? SmokingAllowed { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        public DateTimeOffset? LastRenovationDate { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        public int? Rating { get; set; }

        [IsFilterable, IsSortable]
        public GeographyPoint Location { get; set; }
    }
}
