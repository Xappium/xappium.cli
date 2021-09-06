using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xappium.Tools;
using Xappium.Utilities;

namespace Xappium.BuildSystem
{
    internal class DotNetSdkProjectFile : CSProjFile
    {
        private DotNetTool _dotnetTool { get; }

        public DotNetSdkProjectFile(FileInfo projectFile, DirectoryInfo outputDirectory, DotNetTool dotnetTool)
            : base(projectFile, outputDirectory)
        {
            _dotnetTool = dotnetTool;
        }

        public override OSPlatform Platform => OSPlatform.DotNet;

        public override Task Build(string configuration, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(configuration))
                configuration = "Release";

            // dotnet build HelloForms -t:Run -f net6.0-ios
            return _dotnetTool.Build(b =>
                    b.Add($"{ProjectFile.FullName}")
                     .Add($"--output={OutputDirectory.FullName}")
                     .Add($"--configuration={configuration}"), cancellationToken);
        }

        public override Task<bool> IsSupported()
        {
            return Task.FromResult(true);
        }
    }
}
