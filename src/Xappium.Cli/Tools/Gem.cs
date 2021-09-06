using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Builders;
using Microsoft.Extensions.Logging;

namespace Xappium.Tools
{
    public class Gem
    {
        public static readonly string ToolPath = EnvironmentHelper.GetToolPath("gem");
        private ILogger _logger { get; }

        public Gem(ILogger<Gem> logger)
        {
            _logger = logger;
        }

        public Task Install(string packageName, CancellationToken cancellationToken)
        {
            return ExecuteInternal(b =>
            {
                b.Add("install")
                 .Add(packageName);
            }, cancellationToken);
        }

        public Task InstallXcPretty(CancellationToken cancellationToken) =>
            Install("xcpretty", cancellationToken);

        internal async Task<string> ExecuteInternal(Action<ArgumentsBuilder> configure, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            var toolPath = ToolPath;
            var builder = new ArgumentsBuilder();
            configure(builder);
            var args = builder.Build();
            _logger.LogInformation($"{toolPath} {args}");
            var stdErrBuffer = new StringBuilder();
            var stdOutBuffer = new StringBuilder();
            var stdOut = PipeTarget.Merge(PipeTarget.ToStringBuilder(stdOutBuffer),
                PipeTarget.ToDelegate(l => _logger.LogDebug(l)));

            var result = await Cli.Wrap(toolPath)
                .WithArguments(args)
                .WithValidation(CommandResultValidation.None)
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithStandardOutputPipe(stdOut)
                .ExecuteAsync(cancellationToken);

            var stdErr = stdErrBuffer.ToString().Trim();
            if (!string.IsNullOrEmpty(stdErr))
            {
                if (stdErr.Split('\n').Select(x => x.Trim()).All(x => x.StartsWith("Warning:", StringComparison.InvariantCultureIgnoreCase)))
                    _logger.LogWarning(stdErr);
                else
                    throw new Exception(stdErr);
            }

            return stdOutBuffer.ToString().Trim();
        }
    }
}
