using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Domain.Utils;
using ApiService.Infrastructure.Storage;
using ApiService.IntegrationTests.Setup;
using AutoFixture;
using Elastic.Transport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Testcontainers.Elasticsearch;

namespace ApiService.IntegrationTests;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    private readonly IFixture _fixture;
    private readonly ElasticsearchContainer _elasticsearch
        = new ElasticsearchBuilder().Build();

    public CustomWebApplicationFactory(IFixture fixture)
    {
        _fixture = fixture;
    }

    public IElasticClient GetElasticClient()
    {
        return Services.GetRequiredService<IElasticClient>();
    }

    public IUnitOfWork GetUnitOfWork()
    {
        return Services.GetRequiredService<IUnitOfWork>();
    }

    public ApiDbContext CreateApiDbContext()
    {
        var db = Services.GetRequiredService<IDbContextFactory<ApiDbContext>>().CreateDbContext();
        db.Database.EnsureCreated();

        return db;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");

        builder.ConfigureServices(services =>
        {
            var kafkaTestConfig = _fixture.Create<KafkaTestConfig>();
            services
                .Configure<KafkaOptions>(opts =>
                {
                    opts.PermissionsTopicName = kafkaTestConfig.TopicName;
                    opts.BootstrapServers = kafkaTestConfig.BootstrapServers;
                });

            services
                .AddSingleton<IElasticClient>(provider => GetElasticSearchClient().Result);
        });
    }

    private async Task<ElasticClient> GetElasticSearchClient()
    {
        await _elasticsearch.StartAsync();

        var settings = new ConnectionSettings(new Uri(_elasticsearch.GetConnectionString()))
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
            .DefaultMappingFor<PermissionRecord>(m =>
                m.IdProperty(p => p.Id)
            )
            .DefaultIndex("index-permissions");

        var client = new ElasticClient(settings);
        client.Indices.Create(
            "index-permissions",
            index => index.Map<PermissionRecord>(x => x.AutoMap())
        );

        return client;
    }
}