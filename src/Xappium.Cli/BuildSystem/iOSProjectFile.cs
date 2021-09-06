using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xappium.Apple;
using Xappium.Tools;
using Xappium.Utilities;

namespace Xappium.BuildSystem
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "iOS is named correctly")]
    internal class iOSProjectFile : CSProjFile
    {
        private MSBuild _msBuild { get; }
        private AppleSimulator _appleSimulator { get; }
        private ILogger _logger { get; }

        public iOSProjectFile(FileInfo projectFile, DirectoryInfo outputDirectory, MSBuild msBuild, AppleSimulator appleSimulator, ILogger<iOSProjectFile> logger)
            : base(projectFile, outputDirectory)
        {
            _appleSimulator = appleSimulator;
            _msBuild = msBuild;
            _logger = logger;
        }

        public override OSPlatform Platform => OSPlatform.iOS;

        public override async Task Build(string configuration, CancellationToken cancellationToken)
        {
            var outputPath = OutputDirectory.FullName + Path.DirectorySeparatorChar;
            var props = new Dictionary<string, string>
            {
                { "OutputPath", outputPath },
                { "Configuration", string.IsNullOrEmpty(configuration) ? "Release" : configuration },
                { "Platform", "iPhoneSimulator" }
            };

            // msbuild ../sample/TestApp.iOS/TestApp.iOS.csproj /p:Platform=iPhoneSimulator /p:Configuration=Release /p:OutputPath=$UITESTPATH/bin/
            await _msBuild.Build(ProjectFile.FullName, OutputDirectory.Parent.Parent.FullName, props, cancellationToken).ConfigureAwait(false);
        }

        public override Task<bool> IsSupported()
        {
            return Task.FromResult(EnvironmentHelper.IsIOSSupported(_appleSimulator));
        }
    }
}
