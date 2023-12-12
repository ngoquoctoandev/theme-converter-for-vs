// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;

namespace ThemeConverter.ColorCompiler;

internal abstract class FileWriter
{
    protected FileWriter(ColorManager manager) => ColorManager = manager;

    protected ColorManager ColorManager { get; }

    public abstract void SaveToFile(string fileName);

    public static void SaveColorManagerToFile(ColorManager manager, string fileName, bool registerTheme = false)
    {
        var extension = Path.GetExtension(fileName);
        if (string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase))
        {
            var writer = new XmlFileWriter(manager);
            writer.SaveToFile(fileName);
        }
        else if (string.Equals(extension, ".pkgdef", StringComparison.OrdinalIgnoreCase))
        {
            var writer = new PkgDefWriter(manager);
            writer.SaveToFile(fileName);
        }
        else
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, "Invalid file extension '{0}'. Only XML files and PKGDEF files are allowed.", extension));
        }
    }
}