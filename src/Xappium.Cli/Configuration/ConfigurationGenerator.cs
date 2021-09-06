using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AndroidSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xappium.Android;
using Xappium.Apple;
using Xappium.Utilities;

namespace Xappium.Configuration
{
    internal abstract class ConfigurationGenerator
    {
        private const string ConfigFileName = "uitest.json";
        public const string DefaultUITestEmulatorName = "xappium_emulator_sdk";

        private static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            IgnoreNullValues = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        protected ILogger _logger { get; }

        public ConfigurationGenerator(ILogger logger)
        {
            _logger = logger;
        }

        protected abstract OSPlatform Platform { get; }

        public string HeadBin { get; set; }

        public async Task GenerateTestConfig(string uiTestBin, string configurationPath, string baseWorkingDirectory, bool displayGeneratedConfig, CancellationToken cancellationToken)
        {
            var binDir = new DirectoryInfo(HeadBin);

            var appPath = GetAppPath(binDir);

            var testConfig = Path.Combine(uiTestBin, ConfigFileName);
            var config = BuildBaseConfiguration(uiTestBin, ConfigFileName, configurationPath, testConfig, options, appPath, baseWorkingDirectory);

            await ConfigureForPlatform(config, cancellationToken);

            var jsonOutput = JsonSerializer.Serialize(config, options);
            File.WriteAllText(testConfig, jsonOutput);

            if (displayGeneratedConfig)
                _logger.LogInformation(jsonOutput);
        }

        protected abstract Task ConfigureForPlatform(TestConfiguration config, CancellationToken cancellationToken);

        private TestConfiguration BuildBaseConfiguration(string uiTestBin, string configFileName, string configurationPath, string testConfig, JsonSerializerOptions options, string appPath, string baseWorkingDirectory)
        {
            var config = new TestConfiguration();
            if (!string.IsNullOrEmpty(configurationPath))
            {
                if (!File.Exists(configurationPath))
                    throw new FileNotFoundException($"Could not locate the specified uitest configuration at: '{configurationPath}'");
                config = JsonSerializer.Deserialize<TestConfiguration>(File.ReadAllText(configurationPath), options);
            }
            else if (File.Exists(testConfig))
            {
                config = JsonSerializer.Deserialize<TestConfiguration>(File.ReadAllText(testConfig), options);
            }

            if (config.Capabilities is null)
                config.Capabilities = new Dictionary<string, string>();

            if (config.Settings is null)
                config.Settings = new Dictionary<string, string>();

            config.Platform = Platform.ToString();
            config.AppPath = appPath;

            if (string.IsNullOrEmpty(config.ScreenshotsPath))
                config.ScreenshotsPath = Path.Combine(baseWorkingDirectory, "screenshots");

            return config;
        }

        protected abstract string GetAppPath(DirectoryInfo binDir);
    }

    internal class ConfigurationLoader
    {
        private IServiceProvider _services { get; }

        public ConfigurationLoader(IServiceProvider services)
        {
            _services = services;
        }

        public ConfigurationGenerator Load(string path, OSPlatform platform)
        {
            if(File.Exists(path) && !File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {

            }

            return platform switch
            {
                OSPlatform.Android => _services.GetRequiredService<AndroidConfigurationGenerator>(),// binDir.GetFiles().First(x => x.Name.EndsWith("-Signed.apk")).FullName,
                OSPlatform.iOS => _services.GetRequiredService<iOSConfigurationGenerator>(),// binDir.GetDirectories().First(x => x.Name.EndsWith(".app")).FullName,
                _ => throw new PlatformNotSupportedException($"The {platform} is not supported")
            };
        }
    }

    internal class AndroidConfigurationGenerator : ConfigurationGenerator
    {
        public AndroidConfigurationGenerator(ILogger<AndroidConfigurationGenerator> logger)
            : base(logger)
        {

        }

        protected override OSPlatform Platform => OSPlatform.Android;

        public int? AndroidSdk { get; set; }

        //private static async Task ConfigureForAndroidTests(TestConfiguration config, int? androidSdk, string headBin, CancellationToken cancellationToken)
        protected override async Task ConfigureForPlatform(TestConfiguration config, CancellationToken cancellationToken)
        {
            var androidSdkHome = AndroidSdkManager.FindHome()?.FirstOrDefault();
            if (androidSdkHome is null)
            {
                throw new DirectoryNotFoundException("Could not find the Android Home Directory.");
            }

            var sdkManager = new AndroidSdkManager(androidSdkHome);
            await sdkManager.Acquire();

            // Ensure WebDrivers are installed
            await sdkManager.SdkManager.InstallWebDriver();

            // Ensure latest CmdLine tools are installed
            await sdkManager.SdkManager.InstallLatestCommandLineTools();

            var sdkVersion = ApkHelper.GetAndroidSdkVersion(AndroidSdk, HeadBin);
            _logger.LogInformation($"Targeting Android Sdk: {sdkVersion}");
            var appActivity = ApkHelper.GetAppActivity(HeadBin);

            if (!config.Capabilities.ContainsKey("appActivity"))
                config.Capabilities.Add("appActivity", appActivity);

            var emulatorName = $"{DefaultUITestEmulatorName}{sdkVersion}";

            // Check for connected device
            var devices = sdkManager.Adb.GetDevices();
            if (devices.Any())
            {
                var androidDevice = devices.First();
                var properties = sdkManager.Adb.GetProperties(androidDevice.Serial, "ro.build.version.sdk");
                config.DeviceName = androidDevice.Device;
                config.UDID = androidDevice.Serial;
                config.OSVersion = properties["ro.build.version.sdk"];
                //config.OSVersion = $"{androidDevice.SdkVersion}";
            }
            else
            {
                // Ensure SDK Installed
                await sdkManager.SdkManager.EnsureSdkIsInstalled(sdkVersion);

                // Ensure Emulator Exists
                var emulators = sdkManager.Emulator.ListAvds();
                if (!emulators.Any(x => x == emulatorName))
                    sdkManager.AvdManager.InstallEmulator(emulatorName, sdkVersion);

                // Let Appium Start and control the Emulator
                config.DeviceName = emulatorName;
                config.OSVersion = $"{sdkVersion}";

                if (!config.Capabilities.ContainsKey("avd"))
                    config.Capabilities.Add("avd", emulatorName);
            }
        }

        protected override string GetAppPath(DirectoryInfo binDir) => binDir.GetFiles().First(x => x.Name.EndsWith("-Signed.apk")).FullName;
    }

    internal class iOSConfigurationGenerator : ConfigurationGenerator
    {
        private AppleSimulator _appleSimulator { get; }

        public iOSConfigurationGenerator(ILogger<iOSConfigurationGenerator> logger, AppleSimulator appleSimulator)
            : base(logger)
        {
            _appleSimulator = appleSimulator;
        }

        protected override OSPlatform Platform => OSPlatform.iOS;

        protected override async Task ConfigureForPlatform(TestConfiguration config, CancellationToken cancellationToken)
        {
            // Install Helpers for testing on iOS Devices / Simulators
            // await Pip.UpgradePip(cancellationToken).ConfigureAwait(false);
            // await Gem.InstallXcPretty(cancellationToken).ConfigureAwait(false);
            // await Brew.InstallAppleSimUtils(cancellationToken).ConfigureAwait(false);
            // await Brew.InstallFFMPEG(cancellationToken).ConfigureAwait(false);
            // await Brew.InstallIdbCompanion(cancellationToken).ConfigureAwait(false);
            // await Pip.InstallIdbClient(cancellationToken).ConfigureAwait(false);
            await Task.CompletedTask;

            if (cancellationToken.IsCancellationRequested)
                return;

            var device = _appleSimulator.GetSimulator();
            if (device is null)
                throw new NullReferenceException("Unable to locate the Device");

            config.DeviceName = device.Name;
            config.UDID = device.Udid;
            config.OSVersion = device.OSVersion;
            _appleSimulator.ShutdownAllSimulators();
        }

        protected override string GetAppPath(DirectoryInfo binDir) => binDir.GetDirectories().First(x => x.Name.EndsWith(".app")).FullName;
    }
}
