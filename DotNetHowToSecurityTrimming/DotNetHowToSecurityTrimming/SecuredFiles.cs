using Azure.Search.Documents.Indexes;
using System.Text.Json.Serialization;

public partial class SecuredFiles
{
    [SimpleField(IsKey = true, IsFilterable = true)]
    public string FileId { get; set; }

    [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
    public string Name { get; set; }

    [SimpleField(IsFilterable = true)]
    public string[] GroupIds { get; set; }
}