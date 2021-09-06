using System;
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
    [Command(Name = "test", Description = "Execute the Xappium tests on the iOS Device or Simulator.")]
    internal class iOSTestCommand : TestCommandBase
    {
        private iOSConfigurationGenerator ConfigurationGenerator { get; }
        public iOSTestCommand(ILogger<iOSTestCommand> logger, DotNetTool dotnetTool, Appium appium, Node node, CSProjLoader projectLoader, iOSConfigurationGenerator configurationGenerator)
            : base(logger, dotnetTool, appium, node, projectLoader)
        {
            ConfigurationGenerator = configurationGenerator;
        }

        protected override OSPlatform Platform => OSPlatform.iOS;

        [FileExists]
        [Option(Description = "Specifies the Head Project csproj path for your iOS project.",
            Template = "--app-project")]
        public string iOSProject
        {
            get => DeviceProjectPath;
            set => DeviceProjectPath = value;
        }

        [DirectoryExists]
        [Option(Description = "Specifies the path to the compiled {projectName}.app",
            Template = "--app")]
        public string AppBundle
        {
            get => CompiledAppPath;
            set => CompiledAppPath = value;
        }

        protected override void BeforeExecute()
        {
            if (EnvironmentHelper.IsRunningOnMac)
                throw new PlatformNotSupportedException("You can only build an iOS app on a macOS host.");

            base.BeforeExecute();
        }

        protected override Task GenerateTestConfig(CancellationToken cancellationToken)
        {
            ConfigurationGenerator.HeadBin = HeadBin;
            return ConfigurationGenerator.GenerateTestConfig(UiTestBin, ConfigurationPath, BaseWorkingDirectory, DisplayGeneratedConfig, cancellationToken);
        }
    }
}
