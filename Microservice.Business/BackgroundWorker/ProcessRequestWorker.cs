using System;
using System.Threading;
using System.Threading.Tasks;
using Microservice.Business.Business;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Microservice.Business.BackgroundWorker
{
    public class ProcessRequestWorker : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly int _intervalTime;
        private Timer _timer;

        public ProcessRequestWorker(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _intervalTime = configuration.GetValue<int>("ProcessRequestIntervalTimeInSeconds");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Information("Timed task Process export service starting up");

            _timer = new Timer(
                DoWork,
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(_intervalTime)
            );

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var exportManagement = scope.ServiceProvider.GetService<IExportManagement>();
            exportManagement.ProcessRequest();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information("Timed task Process export service stop requested");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}