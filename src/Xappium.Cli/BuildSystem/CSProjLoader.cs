﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xappium.Apple;
using Xappium.Tools;
using Xappium.Utilities;

namespace Xappium.BuildSystem
{
    public class CSProjLoader
    {
        private static readonly Guid AndroidGuid = Guid.Parse("EFBA0AD7-5A72-4C68-AF49-83D382785DCF");

        private static readonly Guid iOSGuid = Guid.Parse("FEACFBD2-3405-455C-9665-78FE426C6842");

        private IServiceProvider _services { get; }

        public CSProjLoader(IServiceProvider services)
        {
            _services = services;
        }

        public CSProjFile Load(FileInfo projectFile, DirectoryInfo outputDirectory, OSPlatform platform)
        {
            var errorMessage = $"The Project file '{projectFile.FullName}' is not supported";
            var xdoc = new XmlDocument();
            xdoc.Load(projectFile.OpenRead());
            var properties = xdoc.DocumentElement
                .ChildNodes
                .OfType<XmlElement>()
                .Where(x => x.Name == "PropertyGroup")
                .SelectMany(x => x.ChildNodes.OfType<XmlElement>());

            if (xdoc.DocumentElement.Attributes.GetNamedItem("Sdk")?.Value == "Microsoft.NET.Sdk")
            {
                var pattern = $@"(net\d\.\d-{platform})";
                var targetFrameworks = properties.Where(x => x.Name == "TargetFramework" || x.Name == "TargetFrameworks")
                    .SelectMany(x => x.InnerText.Split(';'))
                    .Distinct();

                if (targetFrameworks is null || !targetFrameworks.Any())
                    throw new PlatformNotSupportedException(errorMessage);

                var targetFramework = targetFrameworks.FirstOrDefault(x => Regex.IsMatch(x, pattern, RegexOptions.IgnoreCase));

                if (!string.IsNullOrEmpty(targetFramework))
                {
                    return new DotNetMauiProjectFile(projectFile, outputDirectory, platform, targetFramework, _services.GetRequiredService<DotNetTool>(), _services);
                }
                else if (targetFrameworks.Any(x => x.StartsWith("netcoreapp") || Regex.IsMatch(x, @"^net\d\.\d")))
                {
                    return new DotNetSdkProjectFile(projectFile, outputDirectory, _services.GetRequiredService<DotNetTool>());
                }

                throw new PlatformNotSupportedException(errorMessage);
            }

            var projectTypeGuidsNode = properties.FirstOrDefault(x => x.Name == "ProjectTypeGuids");

            if (projectTypeGuidsNode is null)
                throw new PlatformNotSupportedException(errorMessage);

            var guids = projectTypeGuidsNode.InnerText.Split(';').Select(Parse);

            if (guids.Any(x => x == AndroidGuid))
                return new AndroidProjectFile(projectFile, outputDirectory, _services.GetRequiredService<MSBuild>());
            else if (guids.Any(x => x == iOSGuid))
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    throw new PlatformNotSupportedException("You can only build iOS projects on macOS");

                return new iOSProjectFile(projectFile, outputDirectory, _services.GetRequiredService<MSBuild>(), _services.GetRequiredService<AppleSimulator>(), _services.GetRequiredService<ILogger<iOSProjectFile>>());
            }

            throw new PlatformNotSupportedException(errorMessage);
        }

        private static Guid Parse(string projectTypeGuid)
        {
            var match = Regex.Match(projectTypeGuid, "{(.*)}");
            var value = match.Groups[1].Value;
            return Guid.Parse(value);
        }
    }
}
