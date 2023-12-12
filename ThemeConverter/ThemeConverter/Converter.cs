// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using ThemeConverter.ColorCompiler;
using ThemeConverter.JSON;

namespace ThemeConverter;

public sealed class Converter
{
    private static readonly Guid DarkThemeId  = new("{1ded0138-47ce-435e-84ef-9ec1f439b749}");
    private static readonly Guid LightThemeId = new("{de3dbbcd-f642-433c-8353-8f1df4370aba}");

    private static readonly Lazy<Dictionary<string, ColorKey[]>>      ScopeMappings    = new(ParseMapping.CreateScopeMapping());
    private static readonly Lazy<Dictionary<string, string>>          CategoryGuids    = new(ParseMapping.CreateCategoryGuids());
    private static readonly Lazy<Dictionary<string, string>>          VSCTokenFallback = new(ParseMapping.CreateVSCTokenFallback());
    private static readonly Lazy<Dictionary<string, (float, string)>> OverlayMappings  = new(ParseMapping.CreateOverlayMapping());

    /// <summary>
    ///     Convert the theme file and patch the pkgdef to the target VS if specified.
    /// </summary>
    /// <param name="themeJsonFilePath">The VS Code theme json file path.</param>
    /// <param name="pkgdefOutputPath">Output folder path to write the .pkgdef file to.</param>
    /// <returns>
    ///     Full path to the theme .pkgdef file created in the <paramref name="pkgdefOutputPath" /> folder.
    /// </returns>
    public static string ConvertFile(string themeJsonFilePath, string pkgdefOutputPath)
    {
        var themeName = Path.GetFileNameWithoutExtension(themeJsonFilePath);

        // Parse VS Code theme file and uncomment the code.

        var lines = File.ReadAllLines(themeJsonFilePath);

        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Trim().StartsWith("//"))
            {
                lines[i] = lines[i].Remove(lines[i].IndexOf("//", StringComparison.Ordinal), 2);

                if (!lines[i - 1].EndsWith(',') && !lines[i - 1].EndsWith('{')) lines[i - 1] += ",";
            }

        var text = lines.Aggregate(string.Empty, (current, str) => current + str);

        var jobject = JObject.Parse(text);
        var theme   = jobject.ToObject<ThemeFileContract>();

        if (theme == null)
            throw new Exception("Failed to get theme object.");

        // Group colors by category.
        var colorCategories = GroupColorsByCategory(theme);

        // Compile VS theme.
        var tempPkgdefFilePath = CompileVsTheme(themeName, theme, colorCategories);
        try
        {
            // Copy pkgdef to specified folder
            Directory.CreateDirectory(pkgdefOutputPath);

            var destPkgdefFilePath = Path.Combine(pkgdefOutputPath, $"{themeName}.pkgdef");
            File.Copy(tempPkgdefFilePath, destPkgdefFilePath, true);

            return destPkgdefFilePath;
        }
        finally
        {
            // Delete temporary file.
            File.Delete(tempPkgdefFilePath);
        }
    }

    public static void ValidateDataFiles(Action<string> reportFunc)
    {
        ParseMapping.CheckDuplicateMapping(reportFunc);
    }

    #region Compile VS Theme

    /// <summary>
    ///     Generate the pkgdef from the theme.
    /// </summary>
    /// <param name="themeName">The name of theme.</param>
    /// <param name="theme">The theme object from the json file.</param>
    /// <param name="colorCategories">Colors grouped by category.</param>
    /// <returns>Path to the generated pkgdef</returns>
    private static string CompileVsTheme(
        string themeName,
        ThemeFileContract theme,
        Dictionary<string, Dictionary<string, SettingsContract>> colorCategories)
    {
        using (var tempFileCollection = new TempFileCollection())
        {
            var tempThemeFile = tempFileCollection.AddExtension("vstheme");

            using (var writer = new StreamWriter(tempThemeFile))
            {
                writer.WriteLine("<Themes>");

                var themeGuid = Guid.NewGuid();

                if (theme.Type == "dark")
                    writer.WriteLine($"    <Theme Name=\"{themeName}\" GUID=\"{themeGuid:B}\" FallbackId=\"{DarkThemeId:B}\">");
                else
                    writer.WriteLine($"    <Theme Name=\"{themeName}\" GUID=\"{themeGuid:B}\" FallbackId=\"{LightThemeId:B}\">");

                foreach (var category in colorCategories)
                {
                    writer.WriteLine($"        <Category Name=\"{category.Key}\" GUID=\"{CategoryGuids.Value[category.Key]}\">");

                    foreach (var color in category.Value)
                        if (color.Value.Foreground is not null || color.Value.Background is not null)
                            WriteColor(writer, color.Key, color.Value.Foreground, color.Value.Background);

                    writer.WriteLine("        </Category>");
                }

                writer.WriteLine("    </Theme>");
                writer.WriteLine("</Themes>");
            }

            // Compile the pkgdef
            var reader  = new XmlFileReader(tempThemeFile);
            var manager = reader.ColorManager;

            var tempPkgdef = tempFileCollection.AddExtension("pkgdef", true);
            FileWriter.SaveColorManagerToFile(manager, tempPkgdef, true);

            return tempPkgdef;
        }
    }

    #endregion Compile VS Theme

    #region Translate VS Theme

    /// <summary>
    ///     Group converted colors by category.
    /// </summary>
    /// <param name="theme">the theme contract.</param>
    /// <returns>Mapping from Category to Color Tokens</returns>
    private static Dictionary<string, Dictionary<string, SettingsContract>> GroupColorsByCategory(ThemeFileContract theme)
    {
        // category -> colorKeyName => color value 
        var colorCategories = new Dictionary<string, Dictionary<string, SettingsContract>>();
        // category -> colorKeyName -> assigned by VSC token
        var assignBy = new Dictionary<string, Dictionary<string, string>>();

        var keyUsed = new Dictionary<string, bool>();
        foreach (var key in ScopeMappings.Value.Keys) keyUsed.Add(key, false);

        // Add the editor colors
        if (theme.TokenColors != null)
            foreach (var ruleContract in theme.TokenColors)
            foreach (var scopeName in ruleContract.ScopeNames)
            {
                var scopes = scopeName.Split(',');
                foreach (var scopeRaw in scopes)
                {
                    var scope = scopeRaw.Trim();
                    foreach (var key in ScopeMappings.Value.Keys)
                        if (key.StartsWith(scope) && scope != "")
                            if (ScopeMappings.Value.TryGetValue(key, out var colorKeys))
                            {
                                keyUsed[key] = true;
                                AssignEditorColors(colorKeys, scope, ruleContract, ref colorCategories, ref assignBy);
                            }
                }
            }

        // for keys that were not used during hierarchical assigning, check if there's any fallback that we can use...
        foreach (var key in keyUsed.Keys)
            if (!keyUsed[key])
                if (VSCTokenFallback.Value.TryGetValue(key, out var fallbackToken))
                {
                    // if the fallback is foreground, assign it like a shell color
                    if (fallbackToken == "foreground" && theme.Colors.TryGetValue("foreground", out var color))
                        if (ScopeMappings.Value.TryGetValue(key, out var colorKeys))
                            AssignShellColors(theme, color, colorKeys, ref colorCategories);

                    if (theme.TokenColors != null)
                        foreach (var ruleContract in theme.TokenColors)
                        foreach (var scopeName in ruleContract.ScopeNames)
                        {
                            var scopes = scopeName.Split(',');
                            foreach (var scopeRaw in scopes)
                            {
                                var scope = scopeRaw.Trim();

                                if (fallbackToken.StartsWith(scope) && scope != "")
                                    if (ScopeMappings.Value.TryGetValue(key, out var colorKeys))
                                        AssignEditorColors(colorKeys, scope, ruleContract, ref colorCategories, ref assignBy);
                            }
                        }
                }

        // Add the shell colors
        foreach (var color in theme.Colors)
            if (ScopeMappings.Value.TryGetValue(color.Key.Trim(), out var colorKeyList))
            {
                if (!TryGetColorValue(theme, color.Key, out var colorValue)) continue;

                // calculate the actual border color for editor overlay colors
                if (OverlayMappings.Value.ContainsKey(color.Key) && TryGetColorValue(theme, OverlayMappings.Value[color.Key].Item2, out var backgroundColor)) colorValue = GetCompoundColor(colorValue!, backgroundColor!, OverlayMappings.Value[color.Key].Item1);

                AssignShellColors(theme, colorValue!, colorKeyList, ref colorCategories);
            }

        return colorCategories;
    }

    private static bool TryGetColorValue(ThemeFileContract theme, string token, out string? colorValue)
    {
        theme.Colors.TryGetValue(token, out colorValue);

        var key = token;

        while (colorValue == null)
            if (VSCTokenFallback.Value.TryGetValue(key, out var fallbackToken))
            {
                key = fallbackToken;
                theme.Colors.TryGetValue(key, out colorValue);
            }
            else
            {
                break;
            }

        return colorValue != null;
    }

    /// <summary>
    ///     Compute what is the compound color of 2 overlayed colors with transparency
    /// </summary>
    /// <param name="VSOpacity">What is the opacity that VS will use when displaying this color</param>
    /// <param name="VSCOpacity">The opacity that VSC will apply to this token under special circumstances.</param>
    /// <returns>Color value for VS</returns>
    private static string GetCompoundColor(string overlayColor, string baseColor, float VSOpacity = 1, float VSCOpacity = 1)
    {
        overlayColor = ReviseColor(overlayColor);
        baseColor    = ReviseColor(baseColor);
        var   overlayA = Convert.ToInt32(overlayColor.Substring(0, 2), 16) * VSCOpacity / 255;
        float overlayR = Convert.ToInt32(overlayColor.Substring(2, 2), 16);
        float overlayG = Convert.ToInt32(overlayColor.Substring(4, 2), 16);
        float overlayB = Convert.ToInt32(overlayColor.Substring(6, 2), 16);

        var   baseA = (float)Convert.ToInt32(baseColor.Substring(0, 2), 16) / 255;
        float baseR = Convert.ToInt32(baseColor.Substring(2, 2), 16);
        float baseG = Convert.ToInt32(baseColor.Substring(4, 2), 16);
        float baseB = Convert.ToInt32(baseColor.Substring(6, 2), 16);

        var R = overlayA / VSOpacity * overlayR + (1 - overlayA / VSOpacity) * baseA * baseR;
        var G = overlayA / VSOpacity * overlayG + (1 - overlayA / VSOpacity) * baseA * baseG;
        var B = overlayA / VSOpacity * overlayB + (1 - overlayA / VSOpacity) * baseA * baseB;

        R = Math.Clamp(R, 0, 255);
        G = Math.Clamp(G, 0, 255);
        B = Math.Clamp(B, 0, 255);

        return $"{(int)R:X2}{(int)G:X2}{(int)B:X2}FF";
    }

    private static void AssignEditorColors(ColorKey[] colorKeys,
                                           string scope,
                                           RuleContract ruleContract,
                                           ref Dictionary<string, Dictionary<string, SettingsContract>> colorCategories,
                                           ref Dictionary<string, Dictionary<string, string>> assignBy)
    {
        foreach (var colorKey in colorKeys)
        {
            if (!colorCategories.TryGetValue(colorKey.CategoryName, out var rulesList))
            {
                rulesList                              = new Dictionary<string, SettingsContract>();
                colorCategories[colorKey.CategoryName] = rulesList;
            }

            if (!assignBy.TryGetValue(colorKey.CategoryName, out var assignList))
            {
                assignList                      = new Dictionary<string, string>();
                assignBy[colorKey.CategoryName] = assignList;
            }

            if (rulesList.ContainsKey(colorKey.KeyName))
            {
                if (scope.StartsWith(assignList[colorKey.KeyName]) && ruleContract.Settings.Foreground != null)
                {
                    rulesList[colorKey.KeyName]  = ruleContract.Settings;
                    assignList[colorKey.KeyName] = scope;
                }
            }
            else
            {
                rulesList.Add(colorKey.KeyName, ruleContract.Settings);
                assignList.Add(colorKey.KeyName, scope);
            }
        }
    }

    private static void AssignShellColors(ThemeFileContract theme, string colorValue, ColorKey[] colorKeys, ref Dictionary<string, Dictionary<string, SettingsContract>> colorCategories)
    {
        foreach (var colorKey in colorKeys)
        {
            if (colorKey.ForegroundOpacity is not null && colorKey.VSCBackground is not null)
                if (TryGetColorValue(theme, colorKey.VSCBackground, out var backgroundColor))
                    colorValue = GetCompoundColor(colorValue, backgroundColor!, 1, colorKey.ForegroundOpacity.Value);

            if (!colorCategories.TryGetValue(colorKey.CategoryName, out var rulesList))
            {
                // token name to colors
                rulesList                              = new Dictionary<string, SettingsContract>();
                colorCategories[colorKey.CategoryName] = rulesList;
            }

            if (!rulesList.TryGetValue(colorKey.KeyName, out var colorSetting))
            {
                colorSetting = new SettingsContract();
                rulesList.Add(colorKey.KeyName, colorSetting);
            }

            if (colorKey.IsBackground)
                colorSetting.Background = colorValue;
            else
                colorSetting.Foreground = colorValue;
        }
    }

    #endregion Translate VS Theme

    #region Write VS Theme

    private static void WriteColor(StreamWriter writer, string colorKeyName, string? foregroundColor, string? backgroundColor)
    {
        writer.WriteLine($"            <Color Name=\"{colorKeyName}\">");

        if (backgroundColor is not null) writer.WriteLine($"                <Background Type=\"CT_RAW\" Source=\"{ReviseColor(backgroundColor)}\"/>");

        if (foregroundColor is not null) writer.WriteLine($"                <Foreground Type=\"CT_RAW\" Source=\"{ReviseColor(foregroundColor)}\"/>");

        writer.WriteLine("            </Color>");
    }

    private static string ReviseColor(string color)
    {
        var revisedColor = color.Trim('#');
        switch (revisedColor.Length)
        {
            case 3:
            {
                var r = revisedColor.Substring(0, 1);
                var g = revisedColor.Substring(1, 1);
                var b = revisedColor.Substring(2, 1);
                revisedColor = string.Format("FF{0}{0}{1}{1}{2}{2}", r, g, b);

                break;
            }

            case 4:
            {
                var r = revisedColor.Substring(0, 1);
                var g = revisedColor.Substring(1, 1);
                var b = revisedColor.Substring(2, 1);
                var a = revisedColor.Substring(3, 1);
                revisedColor = string.Format("{0}{0}{1}{1}{2}{2}{3}{3}", a, r, g, b);

                break;
            }

            case 6:
            {
                revisedColor = $"FF{revisedColor}";

                break;
            }

            case 8:
            {
                // go from RRGGBBAA to AARRGGBB
                revisedColor = $"{revisedColor.Substring(6)}{revisedColor.Substring(0, 6)}";

                break;
            }
        }

        return revisedColor;
    }

    #endregion Write VS Theme
}

internal sealed class ColorKey
{
    public ColorKey(string categoryName, string keyName, string backgroundOrForeground, string? foregroundOpacity = null, string? vscBackground = null)
    {
        CategoryName = categoryName;
        KeyName      = keyName;
        Aspect       = backgroundOrForeground;

        IsBackground = backgroundOrForeground.Equals("Background", StringComparison.OrdinalIgnoreCase);

        ForegroundOpacity = foregroundOpacity == null ? null : float.Parse(foregroundOpacity, CultureInfo.InvariantCulture.NumberFormat);
        VSCBackground     = vscBackground;
    }

    public string CategoryName { get; }

    public string KeyName { get; }

    public string Aspect { get; }

    public bool IsBackground { get; }

    public float? ForegroundOpacity { get; }

    public string? VSCBackground { get; }

    public override string ToString() => CategoryName + "&" + KeyName + "&" + Aspect;
}