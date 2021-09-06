using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xappium.Apple;
using Xappium.BuildSystem;
using Xappium.Commands;
using Xappium.Configuration;
using Xappium.Tools;

namespace Xappium
{
    internal class Program
    {
        public static Task<int> Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureLogging(ConfigureLogging)
                .ConfigureServices(ConfigureServices)
                .UseCommandLineApplication<XappiumCommand>(args)
                .Build();

            return host.RunCommandLineApplicationAsync();
        }

        private static void ConfigureLogging(HostBuilderContext ctx, ILoggingBuilder builder)
        {
            builder.AddConsole();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Appium>();
            services.AddSingleton<DotNetTool>();
            services.AddSingleton<MSBuild>();
            services.AddSingleton<Node>();
            services.AddSingleton<CSProjLoader>();
            services.AddSingleton<Gem>();
            services.AddSingleton<Pip>();

            services.AddSingleton<AndroidConfigurationGenerator>();
            services.AddSingleton<TrxReader>();

            if (EnvironmentHelper.IsRunningOnMac)
            {
                services.AddSingleton<iOSConfigurationGenerator>();
                services.AddSingleton<Brew>();
                services.AddSingleton<AppleSimulator>();
            }
        }
    }
}
