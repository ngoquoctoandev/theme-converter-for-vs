﻿using System;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace ThemeConverter.JSON;

[DataContract]
internal class RuleContract
{
    [DataMember(Name = "name", IsRequired = false)] public string Name { get; set; }

    [DataMember(Name = "scope", IsRequired = false)] public JToken Scope { get; set; }

    [DataMember(Name = "settings")] public SettingsContract Settings { get; set; }

    public string[] ScopeNames
    {
        get
        {
            try
            {
                if (Scope is null) return Array.Empty<string>();

                return new[] { Scope.ToObject<string>() };
            }
            catch (Exception)
            {
                return Scope.ToObject<string[]>();
            }
        }
    }
}