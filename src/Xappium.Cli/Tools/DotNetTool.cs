using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Builders;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Xappium.Tools
{
    internal class DotNetTool
    {
        private ILogger _logger { get; }
        private TrxReader _trxReader { get; }

        public DotNetTool(ILogger<DotNetTool> logger, TrxReader trxReader)
        {
            _logger = logger;
            _trxReader = trxReader;
        }

        public async Task Test(string projectPath, string outputPath, string configuration, string resultsDirectory, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(configuration))
                configuration = "Release";

            var baseDirectory = new DirectoryInfo(resultsDirectory).Parent.FullName;
            var logFile = Path.Combine(baseDirectory, "logs", "vstest.log");
            var args = new ArgumentsBuilder().Add("test")
                     .Add($"{projectPath}")
                     .Add($"--output={outputPath}")
                     .Add($"--configuration={configuration}")
                     .Add("--no-build")
                     .Add($"--results-directory={resultsDirectory}")
                     .Add($"--logger:trx;LogFileName={Path.GetFileNameWithoutExtension(projectPath)}.trx")
                     .Add($"--diag:{logFile}")
                     .Build();

            _logger.Log(LogLevel.Information, $"Running dotnet test on '{projectPath}'");
            try
            {
                await Execute(args, LogLevel.Information, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message);
            }
            finally
            {
                ReadTestResults(resultsDirectory);
            }
        }

        private void ReadTestResults(string resultsDirectory)
        {
            try
            {
                var resultsDirectoryInfo = new DirectoryInfo(resultsDirectory);
                if (!resultsDirectoryInfo.Exists)
                    return;

                var trxFileInfo = resultsDirectoryInfo.EnumerateFiles("*.trx").FirstOrDefault();
                if (trxFileInfo is null)
                {
                    return;
                }

                var trx = _trxReader.Load(trxFileInfo);
                _trxReader.LogReport(trx);
            }
            catch(Exception ex)
            {
                _logger.LogWarning("Error reading test results");
                _logger.LogWarning(ex.ToString());
                // suppress errors
            }
        }

        public Task Build(Action<ArgumentsBuilder> configure, CancellationToken cancellationToken)
        {
            var builder = new ArgumentsBuilder()
                .Add("build");
            configure(builder);

            return Execute(builder.Build(), LogLevel.Debug, cancellationToken);
        }

        private async Task Execute(string args, LogLevel logLevel, CancellationToken cancellationToken)
        {
            var cliTool = DotNetExe.FullPath ?? "dotnet";
            _logger.Log(logLevel, $"{cliTool} {args}");

            var stdErrBuffer = new StringBuilder();
            var stdOut = PipeTarget.ToDelegate(l => _logger.Log(logLevel, l));
            var stdErr = PipeTarget.ToStringBuilder(stdErrBuffer);

            var result = await Cli.Wrap(cliTool)
                .WithArguments(args)
                .WithStandardErrorPipe(stdOut)
                .WithStandardErrorPipe(stdErr)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            var error = stdErrBuffer.ToString().Trim();
            if (!string.IsNullOrEmpty(error))
                throw new Exception(error);

            if (result.ExitCode > 0)
                throw new Exception($"The dotnet tool unexpectidly exited without any error output with code: {result.ExitCode}");
        }
    }
}
