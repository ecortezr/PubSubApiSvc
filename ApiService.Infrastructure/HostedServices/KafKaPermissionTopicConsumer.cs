using ApiService.Domain.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiService.Infrastructure.HostedServices
{
    public class KafKaPermissionTopicConsumer : BackgroundService
    {
        private readonly ILogger<KafKaPermissionTopicConsumer> _logger;
        private readonly IKafkaConsumer _kafkaConsumer;

        public KafKaPermissionTopicConsumer(
            ILogger<KafKaPermissionTopicConsumer> logger,
            IKafkaConsumer kafkaConsumer
        )
        {
            _logger = logger;
            _kafkaConsumer = kafkaConsumer;
        }


        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("KafKa Permission Topic Consumer is starting...");

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("KafKa Permission Topic Consumer is stopping...");

            await base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Run(() => StartConsumerAsync(stoppingToken), stoppingToken);
            return Task.CompletedTask;
        }

        private async Task StartConsumerAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("StartConsumerAsync method started!");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await _kafkaConsumer.ConsumePermissionTopicMessage(stoppingToken);
                }

                _logger.LogInformation("StartConsumerAsync method ended!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception executing StartConsumerAsync. Details: {ex}");
            }
        }
    }
}
