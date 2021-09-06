using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xappium.Apple;
using Xappium.Tools;
using Xappium.Utilities;

namespace Xappium.BuildSystem
{
    internal class DotNetMauiProjectFile : CSProjFile
    {
        private DotNetTool _dotnetTool { get; }
        private IServiceProvider _services { get; }

        public DotNetMauiProjectFile(FileInfo projectFile, DirectoryInfo outputDirectory,
            OSPlatform platform, string targetFramework, DotNetTool dotnetTool, IServiceProvider services)
            : base(projectFile, outputDirectory)
        {
            _dotnetTool = dotnetTool;
            _services = services;
            TargetFramework = targetFramework;
            Platform = platform;
        }

        public string TargetFramework { get; }

        public override OSPlatform Platform { get; }

        public override Task Build(string configuration, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(configuration))
                configuration = "Release";

            // dotnet build HelloForms -t:Run -f net6.0-ios
            return _dotnetTool.Build(b =>
                    b.Add($"{ProjectFile.FullName}")
                     .Add($"--framework={TargetFramework}")
                     .Add($"--output={OutputDirectory.FullName}")
                     .Add($"--configuration={configuration}"), cancellationToken);
        }

        public override Task<bool> IsSupported()
        {
            return (Platform) switch
            {
                OSPlatform.Android => Task.FromResult(EnvironmentHelper.IsAndroidSupported),
                OSPlatform.iOS => Task.FromResult(EnvironmentHelper.IsIOSSupported(_services.GetRequiredService<AppleSimulator>())),
                _ => Task.FromResult(false),
            };
        }
    }
}
