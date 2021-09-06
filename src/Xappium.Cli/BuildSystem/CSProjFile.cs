using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xappium.Utilities;

namespace Xappium.BuildSystem
{
    public abstract class CSProjFile
    {
        protected CSProjFile(FileInfo projectFile, DirectoryInfo outputDirectory)
        {
            ProjectFile = projectFile;
            OutputDirectory = outputDirectory;
        }

        public FileInfo ProjectFile { get; }

        public DirectoryInfo OutputDirectory { get; }

        public abstract OSPlatform Platform { get; }

        public abstract Task Build(string configuration, CancellationToken cancellationToken);

        public abstract Task<bool> IsSupported();

        
    }
}
