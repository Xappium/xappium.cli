using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Xappium.BuildSystem;
using Xappium.Tools;
using Xappium.Utilities;

namespace Xappium.Commands.Testing
{
    internal abstract class TestCommandBase : PlatformCommandBase
    {
        private DotNetTool _dotnetTool { get; }
        private Appium _appium { get; }
        private Node _node { get; }
        protected CSProjLoader ProjectLoader { get; }

        protected TestCommandBase(ILogger logger, DotNetTool dotNetTool, Appium appium, Node node, CSProjLoader projectLoader)
            : base(logger)
        {
            _appium = appium;
            _dotnetTool = dotNetTool;
            _node = node;
            ProjectLoader = projectLoader;
        }

        [Option(Description = "Skips running the appium server as part of the tool and assumes another running instance",
            LongName = "skip-appium",
            ShortName = "sa")]
        public bool SkipAppium { get; } = false;

        [Option(Description = "Specifies the address to start appium server listening on.",
            LongName = "appium-address",
            ShortName = "aa")]
        public string AppiumAddress { get; } = "127.0.0.1";

        [Option(Description = "Specifies the port to start appium server listening on.",
            LongName = "appium-port",
            ShortName = "ap")]
        public int AppiumPort { get; } = 4723;

        protected abstract OSPlatform Platform { get; }

        protected override async Task<int> OnExecuteInternal(CancellationToken cancellationToken)
        {
            if (AppiumPort < 80 || AppiumPort > ushort.MaxValue)
                throw new Exception("Specified Appium Port is out of range");

            if (Uri.CheckHostName(AppiumAddress) == UriHostNameType.Unknown)
                throw new Exception("Invalid Appium Address specified.  Must by IP Address or valid host name.");

            if (!_node.IsInstalled)
                throw new Exception("Your environment does not appear to have Node installed. This is required to run Appium");

            await PrepareProjects(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return 0;

            _appium.Address = AppiumAddress;
            _appium.Port = AppiumPort;

            Logger.LogInformation($"Appium {AppiumAddress}:{AppiumPort}");

            if (!SkipAppium)
            {
                Logger.LogInformation($"Installing/running Appium...");

                if (!await _appium.Install(cancellationToken))
                    return 0;

                var appium = await _appium.Run(BaseWorkingDirectory).ConfigureAwait(false);
                Disposables.Add(appium);
            }
            else
            {
                Logger.LogInformation("Appium skipped.");
            }

            await _dotnetTool.Test(UITestProjectPathInfo.FullName, UiTestBin, Configuration?.Trim(), Path.Combine(BaseWorkingDirectory, "results"), cancellationToken)
                .ConfigureAwait(false);

            return 0;
        }

        protected async Task PrepareProjects(CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(HeadBin);
            Directory.CreateDirectory(UiTestBin);

            if (string.IsNullOrEmpty(CompiledAppPath))
            {
                var appProject = ProjectLoader.Load(DeviceProjectPathInfo, new DirectoryInfo(HeadBin), Platform);
                if (!await appProject.IsSupported())
                    throw new PlatformNotSupportedException($"{appProject.Platform} is not supported on this machine. Please check that you have the correct build dependencies.");
                else if (appProject.Platform != Platform)
                    throw new InvalidOperationException($"The platform identitified by the specified project '{appProject.ProjectFile}' is for '{appProject.Platform}'. Expected {Platform}.");

                await appProject.Build(Configuration, cancellationToken).ConfigureAwait(false);
                // TODO: Get this from the appProject.
                CompiledAppPath = string.Empty;
            }

            var uitestProj = ProjectLoader.Load(UITestProjectPathInfo, new DirectoryInfo(UiTestBin), OSPlatform.DotNet);
            await uitestProj.Build(Configuration, cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
                return;

            await GenerateTestConfig(cancellationToken);
        }

        protected abstract Task GenerateTestConfig(CancellationToken cancellationToken);
    }
}