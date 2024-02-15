using ApiService.Domain.Messages;
using Elastic.Transport;
using Nest;
using Testcontainers.Elasticsearch;

namespace ApiService.IntegrationTests.Setup;

public sealed class ElasticsearchSetup : IAsyncLifetime
{
    private ElasticsearchContainer _elasticsearch = new ElasticsearchBuilder()
        .Build();
    private static IElasticClient _elasticClient;
    private static bool _wasStarted;

    public async Task<IElasticClient> GetElasticClient()
    {
        if (_wasStarted)
            return _elasticClient;

        await _elasticsearch.StartAsync();
        _wasStarted = true;

        var settings = new ConnectionSettings(new Uri(_elasticsearch.GetConnectionString()))
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
            .DefaultMappingFor<PermissionRecord>(m =>
                m.IdProperty(p => p.Id)
            )
            .DefaultIndex("index-permissions");

        _elasticClient = new ElasticClient(settings);
        _elasticClient.Indices.Create(
            "index-permissions",
            index => index.Map<PermissionRecord>(x => x.AutoMap())
        );

        return _elasticClient;
    }

    public Task InitializeAsync()
        => _elasticsearch.StartAsync();

    public Task DisposeAsync()
        => _elasticsearch.DisposeAsync().AsTask();
}

