// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ThemeConverter.ColorCompiler;

/// <summary>
///     Writes a ColorManager out to a pkgdef file.
/// </summary>
internal class PkgDefWriter
{
    private readonly ColorManager _colorManager;

    public PkgDefWriter(ColorManager manager) => _colorManager = manager;

    public void SaveToFile(string fileName)
    {
        EnsureDirectoryExists(Path.GetDirectoryName(fileName));

        using var writer = new PkgDefFileWriter(fileName, true);
        WriterRegistration(writer);
        WriterThemes(writer);
    }

    private void WriterRegistration(PkgDefFileWriter writer)
    {
        var item = new PkgDefItem();
        foreach (var theme in _colorManager.Themes)
        {
            if (theme.IsBuiltInTheme)
                continue;

            item.SectionName   = string.Format(CultureInfo.InvariantCulture, @"$RootKey$\Themes\{0:B}", theme.ThemeId);
            item.ValueDataType = PkgDefValueType.PKGDEF_VALUE_STRING;

            item.ValueName       = "@";
            item.ValueDataString = theme.Name;
            writer.Write(item);

            item.ValueName       = "Name";
            item.ValueDataString = theme.Name;
            writer.Write(item);

            if (theme.FallbackId != Guid.Empty)
            {
                item.ValueName       = nameof(theme.FallbackId);
                item.ValueDataString = theme.FallbackId.ToString("B");
                writer.Write(item);
            }
        }
    }

    private void WriterThemes(PkgDefFileWriter writer)
    {
        var entries = new Dictionary<CategoryThemeKey, List<ColorEntry>>();

        foreach (var entry in _colorManager.Themes.SelectMany(t => t.Colors))
        {
            var              key = new CategoryThemeKey(entry.Name.Category.Id, entry.Theme.ThemeId);
            if (!entries.TryGetValue(key, out var keyEntries))
            {
                keyEntries   = new List<ColorEntry>();
                entries[key] = keyEntries;
            }

            keyEntries.Add(entry);
        }

        var item = new PkgDefItem
        {
            ValueDataBinary = new byte[PkgDefConstants.MaxBinaryBlobSize]
        };

        foreach (var unsavedSet in entries)
            if (unsavedSet.Value.Any(e => !e.IsEmpty))
            {
                var category = _colorManager.CategoryIndex[unsavedSet.Key.Category];
                item.SectionName   = string.Format(CultureInfo.InvariantCulture, @"$RootKey$\Themes\{0:B}\{1}", unsavedSet.Key.ThemeId, category.Name);
                item.ValueName     = null;
                item.ValueDataType = PkgDefValueType.PKGDEF_VALUE_STRING;
                writer.Write(item);

                item.ValueName     = "Data";
                item.ValueDataType = PkgDefValueType.PKGDEF_VALUE_BINARY;
                PopulateThemeCategoryInfo(ref item, category.Id, unsavedSet.Value);
                writer.Write(item);
            }
    }

    private void EnsureDirectoryExists(string directory)
    {
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
    }

    private void PopulateThemeCategoryInfo(ref PkgDefItem item, Guid category, List<ColorEntry> colorEntries)
    {
        using var stream = new MemoryStream();
        using (var versionedWriter = new VersionedBinaryWriter(stream))
        {
            var record = new CategoryCollectionRecord();

            var categoryRecord = new CategoryRecord(category);
            record.Categories.Add(categoryRecord);

            foreach (var entry in colorEntries)
                if (!entry.IsEmpty)
                {
                    var colorRecord = CreateColorRecord(entry);
                    categoryRecord.Colors.Add(colorRecord);
                }

            versionedWriter.WriteVersioned(PkgDefConstants.ExpectedVersion, (checkedWriter, version) => { record.Write(checkedWriter); });
        }

        var newBytes = stream.ToArray();
        item.ValueDataBinaryLength = newBytes.Length;
        newBytes.CopyTo(item.ValueDataBinary, 0);
    }

    private ColorRecord CreateColorRecord(ColorEntry entry)
    {
        var colorRecord = new ColorRecord(entry.Name.Name)
        {
            BackgroundType = entry.BackgroundType,
            Background     = entry.BackgroundSource,
            ForegroundType = entry.ForegroundType,
            Foreground     = entry.ForegroundSource
        };

        return colorRecord;
    }
}