// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace ThemeConverter.ColorCompiler;

/// <summary>
///     This class reads Visual Studio XML theme files. It does no verification, so the
///     file should be verified before being passed to this class.
/// </summary>
internal class XmlFileReader
{
    private   ColorManager  _colorManager;
    private   ColorCategory _currentCategory;
    private   ColorName     _currentColor;
    private   ColorEntry    _currentEntry;
    private   ColorTheme    _currentTheme;
    protected string        _fileName;
    private   XmlReader     _reader;

    public XmlFileReader(string fileName) => _fileName = fileName;

    public ColorManager ColorManager
    {
        get
        {
            if (_colorManager == null)
            {
                _colorManager = new ColorManager();
                if (!FileIsEmptyOrNonExistent()) LoadColorManagerFromFile();
            }

            return _colorManager;
        }
    }

    protected void LoadColorManagerFromFile()
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver   = null
        };

        _reader = XmlReader.Create(_fileName, settings);

        while (_reader.Read())
            if (_reader.NodeType == XmlNodeType.Element)
                switch (_reader.Name)
                {
                    case "Theme":
                        ReadThemeElement();

                        break;

                    case "Category":
                        ReadCategoryElement();

                        break;

                    case "Color":
                        ReadColorElement();

                        break;

                    case "Background":
                        ReadBackgroundElement();

                        break;

                    case "Foreground":
                        ReadForegroundElement();

                        break;
                }

        _reader.Close();
    }

    private bool FileIsEmptyOrNonExistent()
    {
        var info = new FileInfo(_fileName);

        return !info.Exists || info.Length == 0;
    }

    private void ReadThemeElement()
    {
        if (Guid.TryParse(_reader.GetAttribute("GUID"), out var guid))
        {
            _currentTheme      = ColorManager.GetOrCreateTheme(guid);
            _currentTheme.Name = _reader.GetAttribute("Name");

            if (Guid.TryParse(_reader.GetAttribute("FallbackId"), out var fallBackguid)) _currentTheme.FallbackId = fallBackguid;
        }
    }

    private void ReadCategoryElement()
    {
        if (Guid.TryParse(_reader.GetAttribute("GUID"), out var guid)) _currentCategory = ColorManager.RegisterCategory(guid, _reader.GetAttribute("Name"));
    }

    private void ReadColorElement()
    {
        _currentColor = new ColorName(_currentCategory, _reader.GetAttribute("Name"));
        _currentEntry = ColorManager.GetOrCreateEntry(_currentTheme.ThemeId, _currentColor);
    }

    private void ReadBackgroundElement()
    {
        if (Enum.TryParse(_reader.GetAttribute("Type"), out __VSCOLORTYPE colorType)) _currentEntry.BackgroundType = colorType;

        if (uint.TryParse(_reader.GetAttribute("Source"), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var source)) _currentEntry.BackgroundSource = SwapARGBandABGR(source, colorType);
    }

    private void ReadForegroundElement()
    {
        if (Enum.TryParse(_reader.GetAttribute("Type"), out __VSCOLORTYPE colorType)) _currentEntry.ForegroundType = colorType;

        if (uint.TryParse(_reader.GetAttribute("Source"), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var source)) _currentEntry.ForegroundSource = SwapARGBandABGR(source, colorType);
    }

    private uint SwapARGBandABGR(uint argb, __VSCOLORTYPE type)
    {
        if (type == __VSCOLORTYPE.CT_RAW)
        {
            var alpha = (byte)(argb >> 24);
            var blue  = (byte)(argb >> 16);
            var green = (byte)(argb >> 8);
            var red   = (byte)argb;

            return (uint)((alpha << 24) | (red << 16) | (green << 8) | blue);
        }

        return argb;
    }
}