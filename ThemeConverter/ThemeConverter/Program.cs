﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Options;

namespace ThemeConverter;

internal sealed class Program
{
    private const string PathToVSThemeFolder = @"Common7\IDE\CommonExtensions\Platform";
    private const string PathToVSExe         = @"Common7\IDE\devenv.exe";

    private static readonly string ProductVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

    private static int Main(string[] args)
    {
        try
        {
            string inputPath = null, outputPath = null, targetVS = null;
            var    showHelp  = false;

            var options = new OptionSet
            {
                { "i|input=", Resources.Input, i => inputPath = i },
                { "o|output=", Resources.Output, o => outputPath = o },
                { "t|targetVS=", Resources.TargetVS, t => targetVS = t },
                { "h|help", Resources.Help, h => showHelp = h != null }
            };

            options.Parse(args);

            if (showHelp)
            {
                Console.WriteLine(string.Format(CultureInfo.CurrentUICulture, Resources.HelpHeader, ProductVersion));
                options.WriteOptionDescriptions(Console.Out);

                return 0;
            }

            Converter.ValidateDataFiles(Console.WriteLine);

            if (inputPath == null || !IsValidPath(inputPath)) throw new ApplicationException(string.Format(CultureInfo.CurrentUICulture, Resources.InputNotExistException, inputPath));

            outputPath ??= GetDirName(inputPath);

            if (targetVS != null && !Directory.Exists(targetVS)) throw new ApplicationException(string.Format(CultureInfo.CurrentUICulture, Resources.TargetVSNotExistException, targetVS));

            Convert(inputPath, outputPath, targetVS);

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine(ex);

            return -1;
        }
    }

    private static string GetDirName(string path) => Directory.Exists(path) ? path : Path.GetDirectoryName(path);

    private static bool IsValidPath(string path) => Directory.Exists(path) || File.Exists(path);

    private static void Convert(string sourcePath, string pkgdefOutputPath, string deployInstall)
    {
        var sourceFiles = Directory.Exists(sourcePath)
            ? Directory.EnumerateFiles(sourcePath, "*.json")
            : Enumerable.Repeat(sourcePath, 1);

        if (!sourceFiles.Any()) throw new ApplicationException(Resources.NoJSONFoundException);

        foreach (var sourceFile in sourceFiles)
        {
            Console.WriteLine($"Converting {sourceFile}");
            Console.WriteLine();

            var pkgdefFilePath = Converter.ConvertFile(sourceFile, pkgdefOutputPath);
            if (!string.IsNullOrEmpty(deployInstall))
            {
                var deployFilePath = Path.Combine(deployInstall, PathToVSThemeFolder, Path.GetFileName(pkgdefFilePath));
                File.Copy(pkgdefFilePath, deployFilePath, true);
            }
        }

        if (!string.IsNullOrEmpty(deployInstall)) LaunchVS(deployInstall);
    }

    private static void LaunchVS(string deployInstall)
    {
        var vsPath = Path.Combine(deployInstall, PathToVSExe);

        if (!File.Exists(vsPath))
            throw new ApplicationException(string.Format(CultureInfo.CurrentUICulture, Resources.TargetDevEnvNotExistException, vsPath));

        Console.WriteLine(Resources.RunningUpdateConfiguration);
        Console.WriteLine();

        var updateConfigProcess = Process.Start(vsPath, "/updateconfiguration");
        updateConfigProcess.WaitForExit();

        if (updateConfigProcess.ExitCode != 0)
            throw new ApplicationException(Resources.UpdateConfigurationException);

        // Launch Visual Studio.
        Console.WriteLine(Resources.LaunchingVS);
        Process.Start(vsPath);
    }
}