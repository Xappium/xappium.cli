using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Builders;
using Microsoft.Extensions.Logging;

namespace Xappium.Tools
{
    public class Brew
    {
        public static readonly string ToolPath = EnvironmentHelper.GetToolPath("brew");

        private ILogger _logger { get; }
        public Brew(ILogger<Brew> logger)
        {
            _logger = logger;
        }

        public Task Install(string packageName, CancellationToken cancellationToken)
        {
            return ExecuteInternal(x => x.Add("install").Add(packageName), cancellationToken);
        }

        public async Task Tap(string source, CancellationToken cancellationToken)
        {
            await ExecuteInternal(x => x.Add("update"), cancellationToken);
            await ExecuteInternal(x => x.Add("tap").Add(source), cancellationToken);
        }

        public async Task InstallIdbCompanion(CancellationToken cancellationToken)
        {
            await Tap("facebook/fb", cancellationToken);
            await Install("idb-companion", cancellationToken);
        }

        public async Task InstallAppleSimUtils(CancellationToken cancellationToken)
        {
            await Tap("wix/brew", cancellationToken);
            await Install("applesimutils", cancellationToken);
        }

        public Task InstallFFMPEG(CancellationToken cancellation) =>
            Install("ffmpeg", cancellation);

        internal async Task ExecuteInternal(Action<ArgumentsBuilder> configure, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var builder = new ArgumentsBuilder();
            configure(builder);
            var args = builder.Build();
            _logger.LogDebug($"{ToolPath} {args}");
            var stdOutBuffer = new StringBuilder();
            var stdOut = PipeTarget.Merge(PipeTarget.ToStringBuilder(stdOutBuffer),
                PipeTarget.ToDelegate(l => _logger.LogInformation(l)));
            var stdError = PipeTarget.ToDelegate(l =>
            {
                if (string.IsNullOrEmpty(l))
                    return;

                // Suppress errors
                _logger.LogWarning(l);
            });

            var result = await Cli.Wrap(ToolPath)
                .WithArguments(args)
                .WithValidation(CommandResultValidation.None)
                .WithStandardErrorPipe(stdError)
                .WithStandardOutputPipe(stdOut)
                .ExecuteAsync(cancellationToken);
        }
    }
}
