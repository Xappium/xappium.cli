using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xappium.Tools;
using Xappium.Utilities;

namespace Xappium.BuildSystem
{
    internal class AndroidProjectFile : CSProjFile
    {
        private MSBuild _msBuild { get; }

        public AndroidProjectFile(FileInfo projectFile, DirectoryInfo outputDirectory, MSBuild msBuild)
            : base(projectFile, outputDirectory)
        {
            _msBuild = msBuild;
        }

        public override OSPlatform Platform => OSPlatform.Android;

        public override async Task Build(string configuration, CancellationToken cancellationToken)
        {
            var props = new Dictionary<string, string>
            {
                { "OutputPath", OutputDirectory.FullName },
                { "Configuration", string.IsNullOrEmpty(configuration) ? "Release" : configuration },
                { "AndroidPackageFormat", "apk" },
                { "AndroidSupportedAbis", "x86" }
            };

            // msbuild ../sample/TestApp.Android/TestApp.Android.csproj /p:Configuration=Release /p:AndroidPackageFormat=apk /p:AndroidSupportedAbis=x86 /p:OutputPath=$UITESTPATH/bin/ /t:SignAndroidPackage
            await _msBuild.Build(ProjectFile.FullName, OutputDirectory.Parent.Parent.FullName, props, cancellationToken, "SignAndroidPackage").ConfigureAwait(false);
        }

        public override Task<bool> IsSupported() =>
            Task.FromResult(EnvironmentHelper.IsAndroidSupported);
    }
}
