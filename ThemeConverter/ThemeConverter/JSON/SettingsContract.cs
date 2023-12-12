using System.Runtime.Serialization;

namespace ThemeConverter.JSON;

[DataContract]
internal class SettingsContract
{
    [DataMember(Name = "foreground", IsRequired = false)] public string Foreground { get; set; }

    [DataMember(Name = "background", IsRequired = false)] public string Background { get; set; }
}