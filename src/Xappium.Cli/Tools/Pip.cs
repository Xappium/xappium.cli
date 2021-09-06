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
    public class Pip
    {
        public static readonly string ToolPath = EnvironmentHelper.GetToolPath("pip3");

        private ILogger _logger { get; }

        public Pip(ILogger<Pip> logger)
        {
            _logger = logger;
        }

        public Task UpgradePip(CancellationToken cancellationToken) =>
            ExecuteInternal("python3", b =>
                b.Add("-m")
                 .Add("pip")
                 .Add("install")
                 .Add("--upgrade")
                 .Add("pip"), cancellationToken);

        public Task Install(string packageName, CancellationToken cancellationToken) =>
            ExecuteInternal(ToolPath, b => b.Add("install").Add(packageName), cancellationToken);

        public Task InstallIdbClient(CancellationToken cancellationToken) =>
            Install("fb-idb", cancellationToken);

        internal async Task<string> ExecuteInternal(string toolPath, Action<ArgumentsBuilder> configure, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

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
