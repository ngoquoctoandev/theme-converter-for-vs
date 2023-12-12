using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ThemeConverter.JSON;

[DataContract]
internal class ThemeFileContract
{
    [DataMember(Name = "type")] public string Type { get; set; } = null!;

    [DataMember(Name = "colors")] public Dictionary<string, string> Colors { get; set; } = new();

    [DataMember(Name = "tokenColors")] public RuleContract[] TokenColors { get; set; } = [];
}