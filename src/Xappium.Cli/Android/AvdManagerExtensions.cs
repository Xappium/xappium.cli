using System.Linq;
using System.Text.RegularExpressions;
using AndroidSdk;

namespace Xappium.Android
{
    internal static class AvdManagerExtensions
    {
        public static void InstallEmulator(this AvdManager avdManager, string emulatorName, int sdkVersion)
        {
            var device = avdManager.ListDevices()
                .Select(x => x.Id)
                .Where(x => Regex.IsMatch(x, @"^pixel_\d$"))
                .OrderByDescending(x => x)
                .FirstOrDefault();

            avdManager.Create(emulatorName, $"{sdkVersion}", device, force: true);
        }
    }
}
