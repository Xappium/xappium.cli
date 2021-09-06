using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using Microsoft.Extensions.Logging;

namespace Xappium.Tools
{
    public class MSBuild
    {
        public readonly string ToolPath;

        private ILogger _logger { get; }

        public MSBuild(ILogger<MSBuild> logger)
        {
            _logger = logger;

            if(EnvironmentHelper.IsRunningOnMac)
            {
                ToolPath = "msbuild";
                return;
            }

            var path = EnvironmentHelper.GetToolPath("msbuild");
            if(!string.IsNullOrEmpty(path))
            {
                ToolPath = path;
                return;
            }

            var vsRootPath = GetRootPath();

            if (string.IsNullOrEmpty(vsRootPath))
                return;

            var installPath = new[]
                {
                    new DirectoryInfo(Path.Combine(vsRootPath, "Enterprise")),
                    new DirectoryInfo(Path.Combine(vsRootPath, "Professional")),
                    new DirectoryInfo(Path.Combine(vsRootPath, "Community")),
                    new DirectoryInfo(Path.Combine(vsRootPath, "Preview")),
                }
                .Where(x => x.Exists)
                .FirstOrDefault();

            if (installPath is null)
                return;

            ToolPath = installPath.EnumerateFiles("msbuild.exe", SearchOption.AllDirectories)
                .Select(x => x.FullName)
                .FirstOrDefault();

            if(!string.IsNullOrEmpty(ToolPath))
                logger.LogInformation($"Using Visual Studio {new DirectoryInfo(vsRootPath).Name} {installPath.Name}");
        }

        private static string GetRootPath()
        {
            return new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio", "2022"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "2022"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "2019")
            }.FirstOrDefault(x => Directory.Exists(x));
        }

        public async Task Build(string projectPath, string baseWorkingDirectory, IDictionary<string, string> props, CancellationToken cancellationToken, string target = null)
        {
            if (string.IsNullOrEmpty(ToolPath))
                throw new Exception("No installation of Visual Studio could be found. Could not locate msbuild.");

            if (!props.ContainsKey("Configuration"))
                props.Add("Configuration", "Release");

            if (!props.ContainsKey("Verbosity"))
                props.Add("Verbosity", "Minimal");

            var stdErrBuffer = new StringBuilder();
            var result = await Cli.Wrap(ToolPath)
                .WithArguments(b =>
                {
                    b.Add(projectPath);

                    b.Add("/r");

                    if (!string.IsNullOrEmpty(target))
                        b.Add($"/t:{target}");

                    foreach ((var key, var value) in props)
                    {
                        b.Add($"/p:{key}={value}");
                    }

                    var logoutput = Path.Combine(baseWorkingDirectory, "logs", $"{Path.GetFileNameWithoutExtension(projectPath)}.binlog");
                    b.Add($"/bl:{logoutput}");
                })
                .WithStandardOutputPipe(PipeTarget.ToDelegate(l => _logger.LogInformation(l)))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            var error = stdErrBuffer.ToString().Trim();
            if (!string.IsNullOrEmpty(error))
                throw new Exception(error);
        }
    }
}
