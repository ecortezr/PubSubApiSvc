using ApiService.Domain.Repositories;
using ApiService.Domain.Utils;
using ApiService.IntegrationTests.Setup;
using AutoFixture;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace ApiService.IntegrationTests;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    private readonly IFixture _fixture;

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
                .AddSingleton<IElasticClient>(provider =>
                {
                    var elasticsearchSetup = new ElasticsearchSetup();

                    return elasticsearchSetup.GetElasticClient().Result;
                });
        });
    }
}