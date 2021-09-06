using System;
using System.IO;

namespace Xappium.Cli.Tests
{
    public class TestEnvironmentHost
    {
        public static readonly string BaseWorkingDirectory = Path.Combine(Environment.CurrentDirectory, "test");
    
        public static void Init()
        {
            var testDir = new DirectoryInfo(BaseWorkingDirectory);
            if (testDir.Exists)
                testDir.Delete(true);

            testDir.Create();

            //Logger.SetWorkingDirectory(testDir.FullName);
        }
    }
}
