// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using Xunit;
using Xunit.Abstractions;

namespace ThemeTests;

public class GetToCodeThemeTest : BaseThemeTest, IClassFixture<GetToCodeThemeTest.Fixture>
{
    public GetToCodeThemeTest(Fixture fixture, ITestOutputHelper outputHelper) : base(fixture, outputHelper)
    {
    }

    [Fact]
    public void StartWindow()
    {
        using (Logger.RunScenario())
        {
            // capture the start window
            var window = App.GetGetToCodeWindow();
            var mru    = Retry.WhileNull(() => window.FindFirstDescendant("MRUItemsListBox")).Result.AsListBox();

            using (var s = Scenario(nameof(StartWindow), window))
            {
                _ = Retry.WhileFalse(() => mru.Items.Any());
                window.HoverButton("_Clone a repository");
                s.Snapshot("Clone.Hover");

                var x = window.FindAllDescendants().Where(d => d.Properties.IsKeyboardFocusable).ToArray();

                for (var i = 0; i < 6; i++)
                {
                    Keyboard.Press(VirtualKeyShort.TAB);
                    Thread.Sleep(50);
                    s.Snapshot("Focus");
                }
            }
        }
    }

    public class Fixture : ThemeTestFixture
    {
        public Fixture(IMessageSink messageSink) : base(messageSink)
        {
        }
    }
}
