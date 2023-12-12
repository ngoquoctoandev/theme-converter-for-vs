// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using Xunit;
using Xunit.Abstractions;

namespace ThemeTests;

public class MainShellThemeTest : BaseThemeTest, IClassFixture<MainShellThemeTest.Fixture>
{
    public MainShellThemeTest(Fixture fixture, ITestOutputHelper outputHelper) : base(fixture, outputHelper)
    {
    }

    [Fact]
    public void MainWindow()
    {
        // setup - dismiss start window
        _ = Retry.While(() => App.GetMainWindow(Automation).ModalWindows.Length, x => x > 0);
        Thread.Sleep(100);
        using (var s = Scenario(nameof(MainWindow)))
        {
            App.GetMainWindow(Automation).Click();
            s.Snapshot("EmptyEnvironment");
        }
    }

    [Fact]
    public void TopLevelMenus()
    {
        var mainWindow = App.GetMainWindow(Automation);
        var menuBar    = mainWindow.FindFirstByXPath("//MenuBar[@AutomationId='MenuBar']").AsMenu();
        using (var s = Scenario(nameof(TopLevelMenus), menuBar))
        {
            s.Snapshot("MainMenu");
            var items = menuBar.Items.Select(s => s.AsMenuItem()).ToArray();

            // single item focused
            items[0].Focus();
            s.Snapshot("MenuFocused", items[0]);

            // each menu expanded
            foreach (var item in items)
            {
                _ = item.Expand();
                // ensure the children items have rendered
                _ = Retry.WhileFalse(() => item.Items.All(y => !y.IsOffscreen));
                s.Snapshot($"{item.Name}.Opened", mainWindow);
                _ = item.Collapse();
            }
        }
    }

    [Fact]
    public void ErrorList()
    {
        var mainWindow        = App.GetMainWindow(Automation);
        var viewMenuItem      = mainWindow.FindFirstByXPath("//MenuItem[@Name='View']").AsMenuItem();
        var errorListMenuItem = viewMenuItem.Items.Single(x => x.Name == "Error List").AsMenuItem();
        _ = errorListMenuItem.Invoke();

        var errorList = Retry.WhileNull(() => mainWindow.FindFirstByXPath("//Pane[contains(@Name, 'Error List')]")).Result;
        using (var scenario = Scenario(nameof(ErrorList), errorList))
        {
            using (var s = Scenario("ScopeCombo", errorList))
            {
                var scopeCombo = errorList.FindFirstByXPath("//ComboBox[@Name='Show items contained by']").AsComboBox();
                s.Snapshot($"{nameof(scopeCombo)}.Default");
                scopeCombo.MoveToElement();
                s.Snapshot($"{nameof(scopeCombo)}.Hovered");
                scopeCombo.Expand();
                _ = Retry.WhileFalse(() => scopeCombo.Items.All(i => !i.IsOffscreen));
                s.Snapshot($"{nameof(scopeCombo)}.Expanded");
                scopeCombo.Items[1].Focus();
                var r = Retry.WhileFalse(() => scopeCombo.Items[1].Properties.HasKeyboardFocus);
                Logger.WriteInfo($"Retries: {r.Iterations}");
                s.Snapshot($"{nameof(scopeCombo)}.ItemFocus");
                // reset
                _ = scopeCombo.Items[0].Select();
                scopeCombo.Collapse();
            }

            using (var s = Scenario("ToolbarButton", errorList))
            {
                var errorButton = errorList.FindFirstByXPath("//Button[contains(@Name, 'Errors')]").AsToggleButton();
                s.Snapshot($"{nameof(errorButton)}.ToggleOn");
                errorButton.MoveToElement();
                s.Snapshot($"{nameof(errorButton)}.Hovered");
                // Seems there is a bug in UIA or maybe VS here - Toggle or Click doesn't update the UI, only
                // a full mouse move and click emulation does the trick
                errorButton.Click(true);
                s.Snapshot($"{nameof(errorButton)}.Clicked");
                errorList.MoveToElement();
                Logger.WriteInfo($"Toggle state: {errorButton.ToggleState}");
                s.Snapshot($"{nameof(errorButton)}.ToggleOff");
                errorButton.Toggle();
                Logger.WriteInfo($"Toggle state: {errorButton.ToggleState}");
            }
        }
    }

    [Fact]
    public void OutputWindow()
    {
        var mainWindow = App.GetMainWindow(Automation);
        try
        {
            // Open a solution so text appears in output window
            App.OpenSolution(@"CSharpApp\CSharpApp.sln");

            var viewMenuItem   = mainWindow.FindFirstByXPath("//MenuItem[@Name='View']").AsMenuItem();
            var outputMenuItem = viewMenuItem.Items.Single(x => x.Name == "Output").AsMenuItem();
            _ = outputMenuItem.Invoke();

            var       outputWindow = Retry.WhileNull(() => mainWindow.FindFirstByXPath("//Pane[contains(@Name, 'Output')]")).Result;
            using var scenario     = Scenario(nameof(OutputWindow), outputWindow);
            scenario.Snapshot("OutputWindow");
        }
        finally
        {
            App.CloseSolution();
        }
    }

    [Fact]
    public void EditorLanguages()
    {
        var mainWindow = App.GetMainWindow(Automation);
        try
        {
            using var scenario = Scenario(nameof(EditorLanguages), mainWindow);

            App.OpenSolution(@"CSharpApp\CSharpApp.sln");

            // Open every file located in the Languages folder
            App.OpenSolutionExplorer();
            var solutionExplorer  = App.GetSolutionExplorer();
            var languagesTreeItem = solutionExplorer.SelectFile("CSharpApp", "Languages");
            var fileNames         = languagesTreeItem.FindAllChildren().Select(element => element.Name);

            foreach (var fileName in fileNames)
            {
                solutionExplorer.OpenFile("CSharpApp", "Languages", fileName);
                var editor = App.GetTextEditor();
                scenario.Snapshot(fileName, editor);
                App.CloseAllTabs();
            }
        }
        finally
        {
            App.CloseSolution();
        }
    }

    [Fact]
    public void ToolsOptionsDialog()
    {
        {
            var mainWindow = App.GetMainVSWindow();
            var toolsMenu  = mainWindow.FindFirstDescendant(cf => cf.ByName("Tools").And(cf.ByControlType(ControlType.MenuItem))).AsMenuItem();
            toolsMenu.Click();
            var optionsMenu = toolsMenu.Items.Single(i => i.Name == "Options...");
            _ = optionsMenu.Invoke();
            var toolsOptions = App.GetOptionsDialogWindow();
            using (var scenario = Scenario(nameof(ToolsOptionsDialog), toolsOptions))
            {
                scenario.Snapshot("Tools Options");
                toolsOptions.Close();
            }
        }
    }

    public class Fixture : ThemeTestFixture
    {
        public Fixture(IMessageSink messageSink) : base(messageSink)
        {
            var window = App.GetGetToCodeWindow();
            window.PressContinueWithoutCodeButton();
        }
    }
}
