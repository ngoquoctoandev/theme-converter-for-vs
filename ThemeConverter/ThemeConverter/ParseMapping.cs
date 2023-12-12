// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThemeConverter;

internal class ParseMapping
{
    private const           string                              TokenColorsName      = "tokenColors";
    private const           string                              VSCTokenName         = "VSC Token";
    private const           string                              VSTokenName          = "VS Token";
    private const           string                              TokenMappingFileName = "TokenMappings.json";
    private static readonly Dictionary<string, ColorKey[]>      ScopeMappings        = new();
    private static readonly Dictionary<string, string>          CategoryGuids        = new();
    private static readonly Dictionary<string, string>          VSCTokenFallback     = new();
    private static          Dictionary<string, (float, string)> OverlayMapping       = new();
    private static readonly List<string>                        MappedVSTokens       = new();

    public static void CheckDuplicateMapping(Action<string> reportFunc)
    {
        var contents = File.ReadAllText(TokenMappingFileName);
        var file     = JObject.Parse(contents);
        var colors   = file[TokenColorsName];

        var addedMappings = new List<string>();

        foreach (var color in colors)
        {
            var VSCToken = color[VSCTokenName];
            var key      = VSCToken.ToString();

            var VSTokens = color[VSTokenName];

            foreach (var VSToken in VSTokens)
                if (addedMappings.Contains(VSToken.ToString()))
                    reportFunc(key + ": " + VSToken);
                else
                    addedMappings.Add(VSToken.ToString());
        }
    }

    public static Dictionary<string, ColorKey[]> CreateScopeMapping()
    {
        var contents = File.ReadAllText(TokenMappingFileName);

        // JObject.Parse will skip JSON comments by default
        var file = JObject.Parse(contents);

        var colors = file[TokenColorsName];
        foreach (var color in colors)
        {
            var VSCToken = color[VSCTokenName];
            var key      = VSCToken.ToString();

            var VSTokens = color[VSTokenName];
            var values   = new List<ColorKey>();
            foreach (var VSToken in VSTokens)
            {
                var      colorKey = VSToken.ToString()?.Split("&");
                ColorKey newColorKey;
                switch (colorKey.Length)
                {
                    case 2: // category & token name (by default foreground)
                        newColorKey = new ColorKey(colorKey[0], colorKey[1], "Foreground");

                        break;

                    case 3: // category & token name & aspect
                        newColorKey = new ColorKey(colorKey[0], colorKey[1], colorKey[2]);

                        break;

                    case 4: // category & token name & vsc opacity & vscode background
                        newColorKey = new ColorKey(colorKey[0], colorKey[1], "Foreground", colorKey[2], colorKey[3]);

                        break;

                    case 5: // category & token name & aspect & vsc opacity & vscode background
                        newColorKey = new ColorKey(colorKey[0], colorKey[1], colorKey[2], colorKey[3], colorKey[4]);

                        break;

                    default:
                        throw new Exception("Invalid mapping format");
                }

                values.Add(newColorKey);
                MappedVSTokens.Add($"{newColorKey.CategoryName}&{newColorKey.KeyName}&{newColorKey.Aspect}");
            }

            ScopeMappings.Add(key, values.ToArray());
        }

        CheckForMissingVSTokens();

        return ScopeMappings;
    }

    private static void CheckForMissingVSTokens()
    {
        if (MappedVSTokens.Count > 0)
        {
            var text            = File.ReadAllText("VSTokens.json");
            var jobject         = JArray.Parse(text);
            var availableTokens = jobject.ToObject<List<string>>();

            var missingVSTokens = new List<string>();

            foreach (var token in availableTokens)
                if (!MappedVSTokens.Contains(token))
                    missingVSTokens.Add(token);

            var json = JsonConvert.SerializeObject(missingVSTokens, Formatting.Indented);
            File.WriteAllText("MissingVSTokens.json", json);
        }
    }

    public static Dictionary<string, string> CreateCategoryGuids()
    {
        var contents = File.ReadAllText("CategoryGuid.json");
        var file     = JsonConvert.DeserializeObject<JObject>(contents);

        foreach (var item in file) CategoryGuids.Add(item.Key, item.Value.ToString());

        return CategoryGuids;
    }

    public static Dictionary<string, string> CreateVSCTokenFallback()
    {
        var contents = File.ReadAllText("VSCTokenFallback.json");
        var file     = JsonConvert.DeserializeObject<JObject>(contents);

        foreach (var item in file) VSCTokenFallback.Add(item.Key, item.Value.ToString());

        return VSCTokenFallback;
    }

    public static Dictionary<string, (float, string)> CreateOverlayMapping()
    {
        var contents = File.ReadAllText("OverlayMapping.json");
        OverlayMapping = JsonConvert.DeserializeObject<Dictionary<string, (float, string)>>(contents);

        return OverlayMapping;
    }
}