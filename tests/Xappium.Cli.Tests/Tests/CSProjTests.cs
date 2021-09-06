using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Xappium.BuildSystem;
using Xappium.Tools;
using Xappium.Utilities;
using Xunit;

namespace Xappium.Cli.Tests
{
    public class CSProjTests
    {
        private IServiceProvider _services { get; }
        private CSProjLoader CSProjLoader { get; }

        public CSProjTests()
        {
            var services = new ServiceCollection();
            services.AddTransient<DotNetTool>();
            services.AddTransient<CSProjLoader>();
            _services = services.BuildServiceProvider();
            CSProjLoader = _services.GetRequiredService<CSProjLoader>();
        }

        [Theory]
        [InlineData("SampleAndroidProject.xml", typeof(AndroidProjectFile))]
        [InlineData("SampleDotNetMultiTargetProject.xml", typeof(DotNetMauiProjectFile))]
        [InlineData("SampleConsoleApp.xml", typeof(DotNetSdkProjectFile))]
        public void FromFileGeneratesCorrectProjectType(string fileName, Type expectedProjectType)
        {
            var filePath = new FileInfo(Path.Combine("Resources", fileName));
            var output = new DirectoryInfo(Path.Combine("test-gen", Path.GetFileNameWithoutExtension(fileName), "bin"));
            CSProjFile proj = null;
            var ex = Record.Exception(() => proj = CSProjLoader.Load(filePath, output, OSPlatform.iOS));

            Assert.Null(ex);
            Assert.NotNull(proj);
            Assert.IsType(expectedProjectType, proj);
        }

        [Fact]
        public void HandlesIOSProjectFile()
        {
            var filePath = new FileInfo(Path.Combine("Resources", "SampleiOSProject.xml"));
            var output = new DirectoryInfo(Path.Combine("test-gen", "SampleiOSProject", "bin"));
            CSProjFile proj = null;
            var ex = Record.Exception(() => proj = CSProjLoader.Load(filePath, output, OSPlatform.iOS));

#if WINDOWS_NT
            Assert.NotNull(ex);
            Assert.IsType<PlatformNotSupportedException>(ex);
#else
            Assert.Null(ex);
            Assert.NotNull(proj);
            Assert.IsType<iOSProjectFile>(proj);
#endif
        }

        [Theory]
        [InlineData("SampleUwpProject.xml")]
        [InlineData("SampleNetStandardProject.xml")]
        public void ThrowsPlatformNotSupportedException(string fileName)
        {
            var filePath = new FileInfo(Path.Combine("Resources", fileName));
            var output = new DirectoryInfo(Path.Combine("test-gen", Path.GetFileNameWithoutExtension(fileName), "bin"));

            CSProjFile proj = null;
            var ex = Record.Exception(() => proj = CSProjLoader.Load(filePath, output, OSPlatform.Other));

            Assert.NotNull(ex);
            Assert.Null(proj);

            Assert.IsType<PlatformNotSupportedException>(ex);
        }
    }
}
