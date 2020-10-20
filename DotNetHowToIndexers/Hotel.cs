using System;
using Microsoft.Spatial;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.Text.Json.Serialization;

namespace AzureSearch.SDKHowTo
{


    // The JsonPropertyName attribute is defined in the Azure Search .NET SDK.
    // Here it used to ensure that Pascal-case property names in the model class are mapped to camel-case
    // field names in the index.
    public partial class Hotel
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        [JsonPropertyName("hotelId")]
        public string HotelId { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        [JsonPropertyName("baseRate")]
        public double? BaseRate { get; set; }

        [SearchableField]
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.FrLucene)]
        [JsonPropertyName("description_fr")]
        public string DescriptionFr { get; set; }

        [SearchableField(IsFilterable = true, IsSortable = true)]
        [JsonPropertyName("hotelName")]
        public string HotelName { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true, IsSortable = true)]
        [JsonPropertyName("category")]
        public string Category { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        [JsonPropertyName("tags")]
        public string[] Tags { get; set; }

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        [JsonPropertyName("parkingIncluded")]
        public bool? ParkingIncluded { get; set; }

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        [JsonPropertyName("smokingAllowed")]
        public bool? SmokingAllowed { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        [JsonPropertyName("lastRenovationDate")]
        public DateTimeOffset? LastRenovationDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        [JsonPropertyName("rating")]
        public int? Rating { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        [FieldBuilderIgnore]
        [JsonPropertyName("location")]
        public GeographyPoint Location { get; set; }
    }
}
