// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThemeConverter.ColorCompiler;

internal class ColorManager
{
    private static readonly Guid                                HighContrastThemeId       = new("a5c004b4-2d4b-494e-bf01-45fc492522c7");
    private static readonly Guid                                LightThemeId              = new("de3dbbcd-f642-433c-8353-8f1df4370aba");
    private static readonly Guid                                DarkThemeId               = new("1ded0138-47ce-435e-84ef-9ec1f439b749");
    private static readonly Guid                                BlueThemeId               = new("a4d6a176-b948-4b29-8c66-53c97a1ed7d0");
    private static readonly Guid                                AdditionalContrastThemeId = new("ce94d289-8481-498b-8ca9-9b6191a315b9");
    private                 ObservableCollection<ColorCategory> _categories;
    private                 Dictionary<Guid, ColorCategory>     _categoryIndex;
    private                 Dictionary<ColorName, ColorRow>     _colorIndex;
    private                 ColorRowCollection                  _colors;
    private                 Dictionary<Guid, ColorTheme>        _themeIndex;

    private ColorThemeCollection _themes;

    public ColorManager()
    {
        AddRequiredThemes();
        _categories = new ObservableCollection<ColorCategory>();
    }

    private IEnumerable<Theme> RequiredThemes
    {
        get
        {
            yield return new Theme(LightThemeId, "Light");
            yield return new Theme(DarkThemeId, "Dark");
            yield return new Theme(BlueThemeId, "Blue");
            yield return new Theme(AdditionalContrastThemeId, "AdditionalContrast");
            yield return new Theme(HighContrastThemeId, "HighContrast");
        }
    }

    public IDictionary<Guid, ColorTheme> ThemeIndex
    {
        get { return _themeIndex = _themeIndex ?? new Dictionary<Guid, ColorTheme>(); }
    }

    public IList<ColorCategory> Categories
    {
        get { return _categories = _categories ?? new ObservableCollection<ColorCategory>(); }
    }

    public IList<ColorTheme> Themes
    {
        get { return _themes = _themes ?? new ColorThemeCollection(this); }
    }

    public IDictionary<Guid, ColorCategory> CategoryIndex
    {
        get { return _categoryIndex = _categoryIndex ?? new Dictionary<Guid, ColorCategory>(); }
    }

    public IDictionary<ColorName, ColorRow> ColorIndex
    {
        get { return _colorIndex = _colorIndex ?? new Dictionary<ColorName, ColorRow>(); }
    }

    public IList<ColorRow> Colors
    {
        get { return _colors = _colors ?? new ColorRowCollection(this); }
    }

    private void AddRequiredThemes()
    {
        foreach (var requiredTheme in RequiredThemes)
        {
            var theme = GetOrCreateTheme(requiredTheme.Guid);
            theme.IsBuiltInTheme = true;

            if (string.IsNullOrEmpty(theme.Name)) theme.Name = requiredTheme.Name;
        }
    }

    public ColorTheme GetOrCreateTheme(Guid themeId)
    {
        if (!ThemeIndex.TryGetValue(themeId, out var theme))
        {
            theme = new ColorTheme(themeId);
            Themes.Add(theme);
        }

        return theme;
    }

    public ColorEntry GetOrCreateEntry(Guid themeId, ColorName name)
    {
        var        theme = GetOrCreateTheme(themeId);
        if (!theme.Index.TryGetValue(name, out var entry))
        {
            entry = new ColorEntry(name);
            theme.Colors.Add(entry);

            if (!ColorIndex.TryGetValue(name, out var row))
            {
                row = new ColorRow(this, name);
                Colors.Add(row);
            }
        }

        return entry;
    }

    public ColorCategory RegisterCategory(Guid categoryId, string name)
    {
        if (!CategoryIndex.TryGetValue(categoryId, out var category))
        {
            category                  = new ColorCategory(categoryId, name);
            CategoryIndex[categoryId] = category;
            Categories.Add(category);
        }

        return category;
    }

    private class Theme
    {
        public Theme(Guid guid, string name)
        {
            Guid = guid;
            Name = name;
        }

        public Guid Guid { get; }

        public string Name { get; }
    }

    private class ColorThemeCollection : OwnershipCollection<ColorTheme>
    {
        private readonly ColorManager _manager;

        public ColorThemeCollection(ColorManager manager) => _manager = manager;

        protected override void TakeOwnership(ColorTheme item)
        {
            _manager.ThemeIndex[item.ThemeId] = item;
            item.Manager                      = _manager;
        }

        protected override void LoseOwnership(ColorTheme item)
        {
            _manager.ThemeIndex.Remove(item.ThemeId);
            item.Manager = null;
        }
    }

    private class ColorRowCollection : OwnershipCollection<ColorRow>
    {
        private readonly ColorManager _manager;

        public ColorRowCollection(ColorManager manager) => _manager = manager;

        protected override void TakeOwnership(ColorRow item)
        {
            _manager.ColorIndex[item.Name] = item;
        }

        protected override void LoseOwnership(ColorRow item)
        {
            _manager.ColorIndex.Remove(item.Name);
        }
    }
}