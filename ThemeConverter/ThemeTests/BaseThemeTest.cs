// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using Xunit.Abstractions;

namespace ThemeTests;

public class BaseThemeTest : BaseTest
{
    private readonly ThemeTestFixture fixture;

    public BaseThemeTest(ThemeTestFixture fixture, ITestOutputHelper outputHelper)
    {
        this.fixture = fixture;
        Logger       = new Logger(App.GetMainWindow(Automation), GetType().Name, outputHelper);
    }

    protected Logger Logger { get; }

    protected Application App => fixture.App;

    protected Logger.Scenario Scenario(string description, AutomationElement element = null)
    {
        return Logger.RunScenario(description, element);
    }
}
