using McMaster.Extensions.CommandLineUtils;
using Xappium.Commands.Testing;

namespace Xappium.Commands
{
    [Command(Name = "ios", Description = "Handles Xappium for iOS")]
    [Subcommand(typeof(iOSTestCommand))]
    public class iOSCommand
    {
        private CommandLineApplication _app { get; }
        private IConsole _console { get; }

        public iOSCommand(CommandLineApplication app, IConsole console)
        {
            _app = app;
            _console = console;
        }

        private void OnExecute()
        {
            if (EnvironmentHelper.IsRunningOnMac)
                _console.WriteLine(_app.GetHelpText());
            else
                _console.WriteLine("The iOS command is only supported on macOS agent. The current host does not support working with an iOS app.");
        }
    }
}
