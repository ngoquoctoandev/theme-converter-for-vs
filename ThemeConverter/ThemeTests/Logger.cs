// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using Xunit.Abstractions;

namespace ThemeTests;

public class Logger
{
    private static readonly string            RootDirectoryForRun = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", DateTime.Now.ToString("yyyy-dd-MM-HHmmss"));
    private readonly        ITestOutputHelper outputHelper;
    private readonly        Stack<Scenario>   scenarios;
    private                 int               count;
    private readonly        bool              diagnostic = false; // Toggle this to true to get more verbose logging
    private                 string            namePrefix;
    private readonly        string            outputPath;

    public Logger(AutomationElement rootScope, string scopeName, ITestOutputHelper outputHelper)
    {
        scenarios         = new Stack<Scenario>();
        RootElement       = rootScope;
        this.outputHelper = outputHelper;
        outputPath        = RootDirectoryForRun;
        namePrefix        = scopeName + ".";
        this.outputHelper.WriteLine($"INFO: Logging to {new Uri(outputPath, UriKind.Absolute)}");
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
    }

    public AutomationElement RootElement { get; set; }

    public Scenario RunScenario([CallerMemberName] string name = null, AutomationElement element = null, bool captureOnDispose = false)
    {
        namePrefix += $"{name}.";
        var scenario = new Scenario(this, name, element, captureOnDispose);
        scenarios.Push(scenario);

        return scenario;
    }

    public void WriteInfo(string error)
    {
        outputHelper.WriteLine($"INFO: {error}");
    }

    public void WriteError(string error)
    {
        outputHelper.WriteLine($"ERROR: {error}");
    }

    public void WriteDiagnostic(string error)
    {
        if (diagnostic)
        {
            outputHelper.WriteLine($"DIAG: {error}");
        }
    }

    public class Scenario : IDisposable
    {
        private readonly bool captureOnDispose;

        public Scenario(Logger scope, string name, AutomationElement element = null, bool captureOnDispose = false)
        {
            Scope                 = scope;
            Name                  = name;
            Element               = element;
            this.captureOnDispose = captureOnDispose;
        }

        public string            Name    { get; }
        public AutomationElement Element { get; set; }
        public Logger            Scope   { get; }

        public void Dispose()
        {
            if (Scope.scenarios.Peek() != this)
            {
                throw new Exception();
            }

            if (captureOnDispose)
            {
                DoCapture("ScenarioEnd");
            }

            var disposedScenario = Scope.scenarios.Pop();
            Scope.namePrefix = Scope.namePrefix.Substring(0, Scope.namePrefix.Length - (disposedScenario.Name.Length + 1));
        }

        public void Snapshot(string description, AutomationElement scopedElement = null)
        {
            DoCapture(description, scopedElement);
        }

        private void DoCapture(string description, AutomationElement scopedElement = null)
        {
            try
            {
                var filename = $"{Scope.namePrefix}{Scope.count++}.{description}.png";
                var filepath = Path.Combine(Scope.outputPath, filename);
                var image    = Capture.Element(scopedElement ?? Element ?? Scope.RootElement);
                Scope.WriteDiagnostic($"URI test: {new Uri(filepath, UriKind.Absolute)}");
                image.ToFile(filepath);
            }
            catch (ExternalException e)
            {
                Scope.WriteError(e.ToString());
            }
        }
    }
}
