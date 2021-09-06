using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Xappium.Apple
{
    internal class AppleSimulator
    {
        private const string AppleSimulatorRuntimeKey = "com.apple.CoreSimulator.SimRuntime.iOS-";

        private ILogger _logger { get; }

        public AppleSimulator(ILogger<AppleSimulator> logger)
        {
            _logger = logger;
        }

        public void ShutdownAllSimulators()
        {
            _logger.LogInformation("Shutting down simulators");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("xcrun", "simctl shutdown all")
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                // don't need to do anything....
            }
        }

        public IEnumerable<AppleDeviceInfo> GetAvailableSimulators()
        {
            _logger.LogInformation("Getting Available Simulators");
            var results = new Dictionary<int, string>();
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("xcrun", "simctl list devices available --json")
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                }
            };
            process.Start();

            var sb = new StringBuilder();
            while(!process.StandardOutput.EndOfStream)
            {
                var line = process.StandardOutput.ReadLine();
                _logger.LogDebug(line);
                sb.Append(line);
            }

            var json = sb.ToString();
            var deviceList = JsonSerializer.Deserialize<AppleDeviceList>(json);

            return deviceList.Devices
                .Where(x => x.Key.StartsWith(AppleSimulatorRuntimeKey))
                .SelectMany(x =>
                {
                    var osVersion = x.Key.Replace(AppleSimulatorRuntimeKey, string.Empty).Replace("-", ".");

                    foreach (var s in x.Value)
                        s.OSVersion = osVersion;
                    return x.Value;
                })
                .Where(x => x.IsAvailable);
        }

        public AppleDeviceInfo GetSimulator()
        {
            var devices = GetAvailableSimulators();
            _logger.LogInformation("Getting Default iPhone Simulator");
            return devices
                .Where(x => !x.Name.Contains("Max") && Regex.IsMatch(x.Name, @"^iPhone \d\d Pro") && x.IsAvailable)
                .OrderByDescending(x => x.OSVersion)
                .ThenByDescending(x => x.Name)
                .FirstOrDefault();
        }
    }
}
