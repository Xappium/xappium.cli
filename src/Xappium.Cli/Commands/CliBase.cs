using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Xappium.Commands
{
    [HelpOption]
    public abstract class CliBase
    {
        protected readonly CompositeDisposable Disposables = new CompositeDisposable();

        protected ILogger Logger { get; }

        protected CliBase(ILogger logger)
        {
            Logger = logger;
        }

        public async Task<int> OnExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                BeforeExecute();
                return await OnExecuteInternal(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return 1;
            }
            finally
            {
                Disposables.Dispose();
            }
        }

        protected virtual void BeforeExecute() { }

        protected abstract Task<int> OnExecuteInternal(CancellationToken cancellationToken);
    }
}
