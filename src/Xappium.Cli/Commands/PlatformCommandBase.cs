using System;
using System.ComponentModel;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Xappium.Utilities;

namespace Xappium.Commands
{
    public abstract class PlatformCommandBase : CliBase
    {
        protected PlatformCommandBase(ILogger logger)
            : base(logger)
        {
        }

        protected FileInfo UITestProjectPathInfo => string.IsNullOrEmpty(UITestProjectPath) ? null : new FileInfo(UITestProjectPath);

        protected string HeadBin
        {
            get
            {
                var headBin = Path.Combine(BaseWorkingDirectory, "bin", "device");
                // HACK: The iOS SDK will mess up the generated app output if a Separator is not at the end of the path.
                headBin += Path.DirectorySeparatorChar;
                return headBin;
            }
        }

        protected string UiTestBin
        {
            get
            {
                var uiTestBin = Path.Combine(BaseWorkingDirectory, "bin", "uitest");
                // HACK: The iOS SDK will mess up the generated app output if a Separator is not at the end of the path.
                uiTestBin += Path.DirectorySeparatorChar;
                return uiTestBin;
            }
        }

        protected FileInfo DeviceProjectPathInfo => string.IsNullOrEmpty(DeviceProjectPath) ? null : new FileInfo(DeviceProjectPath);

        [FileExists]
        [Option(Description = "Specifies the csproj path of the UI Test project",
            LongName = "uitest-project-path",
            ShortName = "uitest")]
        public string UITestProjectPath { get; }


        
        protected string DeviceProjectPath { get; set; }

        protected string CompiledAppPath { get; set; }

        [DefaultValue("Release")]
        [Option(Description = "Specifies the Build Configuration to use on the Platform head and UITest project",
            LongName = "configuration",
            ShortName = "c")]
        public string Configuration { get; } = "Release";

        [Option(Description = "Specifies a UITest.json configuration path that overrides what may be in the UITest project build output directory",
            LongName = "uitest-configuration",
            ShortName = "ui-config")]
        public string ConfigurationPath { get; }

        [Option(Description = "Specifies the test artifact folder",
            LongName = "artifact-staging-directory",
            ShortName = "artifacts")]
        public string BaseWorkingDirectory { get; } = Path.Combine(Environment.CurrentDirectory, "UITest");

        [Option(Description = "Will write the generated uitest.json to the console. This should only be done if you do not have sensative settings that may be written to the console.",
            LongName = "show-config",
            ShortName = "show")]
        public bool DisplayGeneratedConfig { get; }

        [Option(Description = "Skip Running UI Tests")]
        public bool SkipTests { get; set; }

        protected override void BeforeExecute()
        {
            if (Directory.Exists(BaseWorkingDirectory))
                Directory.Delete(BaseWorkingDirectory, true);

            Directory.CreateDirectory(BaseWorkingDirectory);

            var disposable = new DelegateDisposable(() =>
            {
                var binDir = Path.Combine(BaseWorkingDirectory, "bin");
                if (Directory.Exists(binDir))
                    Directory.Delete(binDir, true);
            });
            Disposables.Add(disposable);
        }
    }
}
