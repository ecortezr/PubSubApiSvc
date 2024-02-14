using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiService.Domain.Entities;
using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.IntegrationTests.Setup;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Nest;

namespace ApiService.IntegrationTests.Controller;

public class PermissionControllerTests
{
    [Theory]
    [TestControllerSetup]
    public async Task PermissionController_AddPermission_ValidElasticClient(IElasticClient elasticClient)
    {
        var stats = await elasticClient.PingAsync();

        Assert.True(stats.IsValid);
    }

    [Theory]
    [TestControllerSetup]
    public async Task PermissionController_AddPermission_SuccessfulResponse(HttpClient client, string permissionName)
    {
        var response = await client.PostAsJsonAsync("/Api/Permission", new
        {
            Name = permissionName
        });

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [TestControllerSetup]
    public async Task PermissionController_AddPermission_SuccessfulKafkaPubSub(HttpClient client, IConsumer<Null, string> consumer, string permissionName)
    {
        var response = await client.PostAsJsonAsync("/Api/Permission", new
        {
            Name = permissionName
        });
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var addedPermission = JsonSerializer.Deserialize<PermissionRecord>(jsonResponse);

        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(5));
        var consumedPermission = JsonSerializer.Deserialize<PermissionTopicMessage>(consumeResult.Message.Value);

        Assert.NotNull(addedPermission);
        Assert.NotNull(consumedPermission);
        Assert.Equal(
            NameOperationEnum.request,
            consumedPermission?.NameOperation
        );
        Assert.Equal(
            addedPermission,
            consumedPermission?.Permission
        );
    }

    [Theory]
    [TestControllerSetup]
    public async Task PermissionController_AddPermission_SuccessfulStoreOnDb(HttpClient client, IUnitOfWork unitOfWork, string permissionName)
    {
        var response = await client.PostAsJsonAsync("/Api/Permission", new
        {
            Name = permissionName
        });
        var dbEntry = await unitOfWork.Set<Permission>()
            .FirstOrDefaultAsync(p =>
                p.Name == permissionName
            );

        Assert.NotNull(dbEntry);
    }

    [Theory]
    [TestControllerSetup]
    public async Task PermissionController_AddPermission_SuccessfulStoreOnElasticsearch(HttpClient client, IElasticClient elasticClient, string permissionName)
    {
        var response = await client.PostAsJsonAsync("/Api/Permission", new
        {
            Name = permissionName
        });
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var addedPermission = JsonSerializer.Deserialize<PermissionRecord>(jsonResponse);
        Assert.NotNull(addedPermission);

        await Task.Delay(10000);
        var elasticResponse = await elasticClient.GetAsync<PermissionRecord>(addedPermission?.Id);

        Assert.True(elasticResponse.IsValid);
    }
}
