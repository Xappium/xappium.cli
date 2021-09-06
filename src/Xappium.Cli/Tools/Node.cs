using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using Microsoft.Extensions.Logging;

namespace Xappium.Tools
{
    public class Node
    {
        private ILogger _logger { get; }

        public Node(ILogger<Node> logger)
        {
            _logger = logger;
        }

        public string Version
        {
            get
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo("node", "-v")
                    {
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    },
                };
                process.Start();
                while(!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (line.StartsWith("v"))
                    {
                        _logger.LogInformation($"Node: {line} installed");
                        return line;
                    }
                }

                return null;
            }
        }

        public bool IsInstalled => !string.IsNullOrEmpty(Version);

        public async Task<bool> InstallPackage(string packageName, CancellationToken cancellationToken)
        {
            var toolPath = EnvironmentHelper.GetToolPath("npm");
            _logger.LogInformation($"{toolPath} install -g {packageName}");
            var isMac = EnvironmentHelper.IsRunningOnMac;
            var errorLines = new List<string>();
            var stdOut = PipeTarget.ToDelegate(l => _logger.LogInformation(l));
            var stdErr = PipeTarget.ToDelegate(l =>
            {
                if (string.IsNullOrEmpty(l) || (isMac && l.Contains("did not detect a Windows system")))
                    return;
                errorLines.Add(l);
            });
            await Cli.Wrap(toolPath)
                //.WithArguments($"install -g {packageName}")
                .WithArguments(b => b.Add("install")
                                .Add("-g")
                                .Add(packageName))
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(stdOut)
                .WithStandardErrorPipe(stdErr)
                .ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return false;

            if (errorLines.Any())
                return true;

            throw new Exception(string.Join(Environment.NewLine, errorLines));
        }
    }
}
