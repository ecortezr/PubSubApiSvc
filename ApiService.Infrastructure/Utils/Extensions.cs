using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Infrastructure.HostedServices;
using ApiService.Infrastructure.Storage;
using Elasticsearch.Net;
using Microsoft.Data.Sqlite;
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

            // Elasticsearch
            var url = $"{configuration["ELKConfiguration:Url"]}";
            var username = $"{configuration["ELKConfiguration:Username"]}";
            var password = $"{configuration["ELKConfiguration:Password"]}";
            var defaultIndex = $"{configuration["ELKConfiguration:Index"]}";

            var settings = new ConnectionSettings(new Uri(url))
                .BasicAuthentication(username, password)
                .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
                .PrettyJson()
                .EnableApiVersioningHeader()
                .DefaultIndex(defaultIndex)
                .DefaultMappingFor<PermissionRecord>(m =>
                    m.IdProperty(p => p.Id)
                );

            var client = new ElasticClient(settings);
            client.Indices.Create(
                defaultIndex,
                index => index.Map<PermissionRecord>(x => x.AutoMap())
            );

            services.AddSingleton<IElasticClient>(client);

            return AddCommonServices(services);
        }

        public static IServiceCollection AddInfrastructureForIntegrationTests(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services = AddDbContextWithSqlite(services);

            return AddCommonServices(services);
        }

        public static IServiceCollection AddInfrastructureForUnitTests(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return AddDbContextWithSqlite(services)
                .AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static IServiceCollection AddDbContextWithSqlite(IServiceCollection services)
        {
            // DB context
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            services.AddDbContext<ApiDbContext>(opt =>
                          opt.UseSqlite(connection));

            return services;
        }

        private static IServiceCollection AddCommonServices(IServiceCollection services)
        {
            // Repository
            services
                .AddScoped<IUnitOfWork, UnitOfWork>();

            // Kafka Producer
            services
                .AddSingleton<IKafkaProducer, KafkaProducer>();

            // Kafka Consumer
            services
                .AddSingleton<IKafkaConsumer, KafkaConsumer>();

            // Hosted services
            services
                .AddHostedService<KafKaPermissionTopicConsumer>();

            return services;
        }
    }
}

