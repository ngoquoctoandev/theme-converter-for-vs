// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace ThemeConverter.ColorCompiler;

internal class PkgDefFileWriter : IDisposable
{
    private          bool         disposedValue;
    private readonly StreamWriter file;
    private readonly bool         isOpen;
    private          string       lastSectionWritten;

    public PkgDefFileWriter(string filePath, bool overwriteExisting)
    {
        file               = new StreamWriter(filePath, !overwriteExisting, Encoding.UTF8);
        isOpen             = true;
        lastSectionWritten = "";
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool Write(PkgDefItem item)
    {
        if (!isOpen)
            return false;

        if (string.IsNullOrEmpty(item.SectionName))
            return false;

        if (item.SectionName != lastSectionWritten)
        {
            if (lastSectionWritten != string.Empty) file.WriteLine();
            var line = string.Format("{0}{1}{2}",
                                     Constants.SectionStartChar,
                                     item.SectionName,
                                     Constants.SectionEndChar);
            file.WriteLine(line);
            lastSectionWritten = item.SectionName;
        }

        if (!string.IsNullOrEmpty(item.ValueName))
        {
            if (item.ValueName == "@")
            {
                file.Write(item.ValueName);
            }
            else
            {
                var line = $"\"{item.ValueName}\"";
                file.Write(line);
            }

            file.Write("=");

            switch (item.ValueDataType)
            {
                //Todo: catch invalid cast exceptions, report, and continue;
                case PkgDefValueType.PKGDEF_VALUE_STRING:
                {
                    var line = $"\"{item.ValueDataString}\"";
                    file.Write(line);

                    break;
                }

                case PkgDefValueType.PKGDEF_VALUE_BINARY:
                {
                    var line = string.Format("{0}{1}",
                                             Constants.BinaryPrefix,
                                             DataToHexString(item.ValueDataBinary, item.ValueDataBinaryLength));
                    file.Write(line);

                    break;
                }
            }

            file.WriteLine();
        }

        return true;
    }

    private string DataToHexString(byte[] binaryData, int length)
    {
        if (!isOpen)
            return null;

        var dataString = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            dataString.Append(binaryData[i].ToString("x2"));
            dataString.Append(",");
        }

        return dataString.ToString().TrimEnd(',');
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
                // Dispose managed resources
                file?.Dispose();

            disposedValue = true;
        }
    }

    private class Constants
    {
        public const string SectionStartChar = @"[";
        public const string SectionEndChar   = @"]";
        public const string BinaryPrefix     = "hex:";
    }
}