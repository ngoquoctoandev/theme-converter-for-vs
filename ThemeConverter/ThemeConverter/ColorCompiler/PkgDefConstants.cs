﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace ThemeConverter.ColorCompiler;

internal static class PkgDefConstants
{
    public const           int    MaxBinaryBlobSize          = 1000000;
    public const           string DataValueName              = "Data";
    public const           int    ExpectedVersion            = 11;
    public static readonly Regex  FindThemeExpression        = new(@"\$RootKey\$\\Themes\\(?'name'[^\\]*)", RegexOptions.Singleline);
    public static readonly Regex  FindCategoryNameExpression = new(@"\$RootKey\$\\Themes\\(?'name'[^\\]*)\\(?'categoryName'[^\\]*)", RegexOptions.Singleline);
}