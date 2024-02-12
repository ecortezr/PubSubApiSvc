using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Infrastructure.HostedServices;
using ApiService.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace ApiService.Infrastructure.Utils
{
    public static class Extensions
	{
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // DB context
            services.AddDbContext<ApiDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("SqlServer"),
                    contextOptionsBuilder =>
                    {
                        contextOptionsBuilder.CommandTimeout(int.Parse(configuration["DatabaseConfiguration:SqlServerCommandTimeout"] ?? "120"));
                        contextOptionsBuilder.EnableRetryOnFailure(
                            maxRetryCount: int.Parse(configuration["DatabaseConfiguration:SqlServerMaxRetryCount"] ?? "3"),
                            maxRetryDelay: TimeSpan.FromSeconds(int.Parse(configuration["DatabaseConfiguration:SqlServerMaxRetryDelay"] ?? "30")),
                            errorNumbersToAdd: null
                        );
                    });
            });

            // Repository
            services
                .AddScoped<IUnitOfWork, UnitOfWork>();

            // Kafka Producer
            services
                .AddSingleton<IKafkaProducer, KafkaProducer>();

            // Hosted services
            services
                .AddHostedService<KafKaPermissionTopicConsumer>();

            // Elasticsearch
            var url = $"{configuration["ELKConfiguration:Url"]}";
            var username = $"{configuration["ELKConfiguration:Username"]}";
            var password = $"{configuration["ELKConfiguration:Password"]}";
            var defaultIndex = $"{configuration["ELKConfiguration:Index"]}";

            var settings = new ConnectionSettings(new Uri(url))
                .BasicAuthentication(username, password)
                .PrettyJson()
                .EnableApiVersioningHeader()
                .DefaultIndex(defaultIndex);

            AddDefaultELKMappings(settings);

            var client = new ElasticClient(settings);

            services.AddSingleton<IElasticClient>(client);

            CreateELKIndex(client, defaultIndex);

            return services;
        }

        private static void AddDefaultELKMappings(ConnectionSettings settings)
        {
            settings
                .DefaultMappingFor<PermissionRecord>(m =>
                    m.IdProperty(p => p.Id)
                );
        }

        private static void CreateELKIndex(IElasticClient client, string indexName)
        {
            client.Indices.Create(
                indexName,
                index => index.Map<PermissionRecord>(x => x.AutoMap())
            );
        }
    }
}

