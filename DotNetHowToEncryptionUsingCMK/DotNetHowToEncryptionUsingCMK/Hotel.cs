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
        public string HotelId { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public double? BaseRate { get; set; }

        [SearchableField]
        public string Description { get; set; }

        [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.FrLucene)]
        public string DescriptionFr { get; set; }

        [SearchableField(IsFilterable = true, IsSortable = true)]
        public string HotelName { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true, IsSortable = true)]
        public string Category { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string[] Tags { get; set; }

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public bool? ParkingIncluded { get; set; }

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public bool? SmokingAllowed { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public DateTimeOffset? LastRenovationDate { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public int? Rating { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public GeographyPoint Location { get; set; }
    }
}
