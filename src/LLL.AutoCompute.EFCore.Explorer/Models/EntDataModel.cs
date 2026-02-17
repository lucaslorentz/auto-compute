using System.Text.Json;

namespace LLL.AutoCompute.EFCore.Explorer.Models;

public class EntDataModel
{
    public required object Id { get; set; }
    public Dictionary<string, JsonElement> PropertyValues { get; set; } = [];
    public Dictionary<string, EntityReferenceModel?> ReferenceValues { get; set; } = [];
    public Dictionary<string, JsonElement> ComputedValues { get; set; } = [];
    public Dictionary<string, JsonElement> MethodValues { get; set; } = [];
    public Dictionary<string, JsonElement> MembersConsistency { get; set; } = [];
}
