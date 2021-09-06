using System.Threading.Tasks;
using AndroidSdk;

namespace Xappium.Android
{
    internal static class AndroidSdkManagerExtensions
    {
        public static async Task InstallWebDriver(this SdkManager sdkManager)
        {
            await sdkManager.Acquire();
            sdkManager.Install("extras;google;webdriver");
        }

        public static async Task InstallLatestCommandLineTools(this SdkManager sdkManager)
        {
            await sdkManager.Acquire();
            sdkManager.Install("cmdline-tools;latest");
        }

        public static async Task EnsureSdkIsInstalled(this SdkManager sdkManager, int sdkVersion)
        {
            await sdkManager.Acquire();
            sdkManager.Install("system-images;android-{sdkVersion};google_apis_playstore;x86");
        }
    }
}
