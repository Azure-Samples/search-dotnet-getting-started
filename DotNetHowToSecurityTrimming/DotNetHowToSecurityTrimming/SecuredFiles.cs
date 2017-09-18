using System;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Spatial;
using Newtonsoft.Json;

// The SerializePropertyNamesAsCamelCase attribute is defined in the Azure Search .NET SDK.
// It ensures that Pascal-case property names in the model class are mapped to camel-case
// field names in the index.
[SerializePropertyNamesAsCamelCase]
public partial class SecuredFiles
{
    [System.ComponentModel.DataAnnotations.Key]
    [IsFilterable]
    public string FileId { get; set; }

    [IsSearchable, IsFilterable, IsSortable]
    public string Name { get; set; }

    [IsFilterable, IsSortable, IsFacetable]
    public int Size { get; set; }

    [IsFilterable, IsSortable, IsFacetable]
    public string Type { get; set; }

    [IsFilterable]
    public string[] GroupIds { get; set; }
}