using McMaster.Extensions.CommandLineUtils;
using Xappium.Commands.Testing;

namespace Xappium.Commands
{
    [HelpOption]
    [Command(Description = "Handles Xappium for Android apps")]
    [Subcommand(typeof(AndroidTestCommand))]
    public class AndroidCommand
    {
        private CommandLineApplication _app { get; }
        private IConsole _console { get; }

        public AndroidCommand(CommandLineApplication app, IConsole console)
        {
            _app = app;
            _console = console;
        }

        private void OnExecute() => _console.WriteLine(_app.GetHelpText());
    }
}
