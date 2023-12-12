// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using FlaUI.Core;
using FlaUI.Core.Input;
using Microsoft.VisualStudio.Setup.Configuration;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ThemeTests;

public abstract class ThemeTestFixture : IDisposable
{
    private readonly double storedMovePixelsPerMillisecond;

    public ThemeTestFixture(IMessageSink messageSink)
    {
        var devenvPath = GetPathToTargetVisualStudioInstall(messageSink);
        App                            = Application.Launch(devenvPath);
        storedMovePixelsPerMillisecond = Mouse.MovePixelsPerMillisecond;
        Mouse.MovePixelsPerMillisecond = 10;
    }

    public Application App { get; }

    public void Dispose()
    {
        _ = App?.Close();
        App?.Dispose();
        Mouse.MovePixelsPerMillisecond = storedMovePixelsPerMillisecond;
    }

    public static string GetPathToTargetVisualStudioInstall(IMessageSink messageSink)
    {
        var vsInstallDir = Environment.GetEnvironmentVariable("VSINSTALLDIR");
        var reason       = string.Empty;

        if (!string.IsNullOrEmpty(vsInstallDir) && Directory.Exists(vsInstallDir))
        {
            reason = "VSINSTALLDIR environment variable.";
        }
        else
        {
            var setupConfig    = new SetupConfiguration();
            var setupInstances = setupConfig.EnumAllInstances();
            var instances      = new ISetupInstance[1];

            setupInstances.Next(instances.Length, instances, out var fetched);

            if (fetched != 1)
            {
                throw new Exception("Could not find a VS install to target");
            }

            vsInstallDir = instances[0].GetInstallationPath();
            reason       = $"instance ID {instances[0].GetInstanceId()} being first in SetupConfiguration.";
            if (!Directory.Exists(vsInstallDir))
            {
                throw new Exception($"Could not find devenv.exe at {vsInstallDir}");
            }
        }

        var devenvPath = Path.Combine(vsInstallDir, @"Common7\IDE\devenv.exe");
        messageSink.OnMessage(new DiagnosticMessage($"Targeting {devenvPath} by way of {reason}"));

        return devenvPath;
    }

    protected virtual void DisposeInternal()
    {
    }
}
