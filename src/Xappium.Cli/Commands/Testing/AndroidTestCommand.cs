using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Xappium.BuildSystem;
using Xappium.Configuration;
using Xappium.Tools;
using Xappium.Utilities;

namespace Xappium.Commands.Testing
{
    [Command(Name = "test", Description = "Execute the Xappium tests on the Android Device or Emulator.")]
    internal class AndroidTestCommand : TestCommandBase
    {
        private AndroidConfigurationGenerator ConfigurationGenerator { get; }
        public AndroidTestCommand(ILogger<AndroidTestCommand> logger, DotNetTool dotnetTool, Appium appium, Node node, CSProjLoader projectLoader)
            : base(logger, dotnetTool, appium, node, projectLoader)
        {
        }

        [Option(Description = "Specifies the Android SDK version to ensure is installed for the Emulator",
            LongName = "android-sdk",
            ShortName = "droid")]
        public int? AndroidSdk { get; }

        [FileExists]
        [Option(Description = "Specifies the Head Project csproj path for your Android project.",
            Template = "--app-project")]
        public string AndroidProject
        {
            get => DeviceProjectPath;
            set => DeviceProjectPath = value;
        }

        [FileExists]
        [Option(Description = "Specifies the path to the compiled apk",
            Template = "--apk")]
        public string AppBundle
        {
            get => CompiledAppPath;
            set => CompiledAppPath = value;
        }

        protected override OSPlatform Platform => OSPlatform.Android;

        protected override Task GenerateTestConfig(CancellationToken cancellationToken)
        {
            ConfigurationGenerator.AndroidSdk = AndroidSdk;
            ConfigurationGenerator.HeadBin = HeadBin;
            return ConfigurationGenerator.GenerateTestConfig(UiTestBin, ConfigurationPath, BaseWorkingDirectory, DisplayGeneratedConfig, cancellationToken);
        }
    }
}
