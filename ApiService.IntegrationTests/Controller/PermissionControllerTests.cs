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
    public async Task PermissionController_Infrastructure_ValidElasticClient(IElasticClient elasticClient)
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

        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));
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

    [Theory]
    [TestControllerSetup]
    public async Task PermissionController_EditPermission_SuccessfulResponse(HttpClient client, string permissionName)
    {
        // Add a new permission
        _ = await client.PostAsJsonAsync("/Api/Permission", new
        {
            Name = "Initial name"
        });

        // Edit permission name
        var response = await client.PutAsJsonAsync("/Api/Permission/1", new
        {
            Name = permissionName
        });

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [TestControllerSetup]
    public async Task PermissionController_EditPermission_SuccessfulKafkaPubSub(HttpClient client, IConsumer<Null, string> consumer, string permissionName)
    {
        // Add a new permission
        _ = await client.PostAsJsonAsync("/Api/Permission", new
        {
            Name = "Initial name"
        });

        // Edit permission name
        var response = await client.PutAsJsonAsync("/Api/Permission/1", new
        {
            Name = permissionName
        });
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var updatedPermission = JsonSerializer.Deserialize<PermissionRecord>(jsonResponse);

        // Consume request operation
        _ = consumer.Consume(TimeSpan.FromSeconds(10));
        // Consume modified operation
        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(2));
        var consumedPermission = JsonSerializer.Deserialize<PermissionTopicMessage>(consumeResult.Message.Value);

        Assert.NotNull(updatedPermission);
        Assert.NotNull(consumedPermission);
        Assert.Equal(
            NameOperationEnum.modified,
            consumedPermission?.NameOperation
        );
        Assert.Equal(
            updatedPermission,
            consumedPermission?.Permission
        );
    }

    [Theory]
    [TestControllerSetup]
    public async Task PermissionController_EditPermission_SuccessfulStoreOnDb(HttpClient client, IUnitOfWork unitOfWork, string permissionName)
    {
        // Add a new permission
        _ = await client.PostAsJsonAsync("/Api/Permission", new
        {
            Name = "Initial name"
        });

        // Edit permission name
        var response = await client.PutAsJsonAsync("/Api/Permission/1", new
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
    public async Task PermissionController_EditPermission_SuccessfulStoreOnElasticsearch(HttpClient client, IElasticClient elasticClient, string permissionName)
    {
        // Add a new permission
        _ = await client.PostAsJsonAsync("/Api/Permission", new
        {
            Name = "Initial name"
        });

        // Edit permission name
        var response = await client.PutAsJsonAsync("/Api/Permission/1", new
        {
            Name = permissionName
        });
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var updatedPermission = JsonSerializer.Deserialize<PermissionRecord>(jsonResponse);
        Assert.NotNull(updatedPermission);

        await Task.Delay(10000);
        var elasticResponse = await elasticClient.GetAsync<PermissionRecord>(updatedPermission?.Id);

        Assert.True(elasticResponse.IsValid);
    }

    [Theory]
    [TestControllerSetup]
    public async Task PermissionController_GetPermission_SuccessfulKafkaPubSub(HttpClient client, IConsumer<Null, string> consumer)
    {
        _ = await client.GetAsync("/Api/Permission");

        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));
        var consumedPermission = JsonSerializer.Deserialize<PermissionTopicMessage>(consumeResult.Message.Value);

        Assert.NotNull(consumedPermission);
        Assert.Equal(
            NameOperationEnum.get,
            consumedPermission?.NameOperation
        );
        Assert.Null(consumedPermission?.Permission);
    }

    [Theory]
    [TestControllerSetup]
    public async Task PermissionController_GetPermissions_SuccessfulRecoverFromElasticsearch(HttpClient client, IElasticClient elasticClient)
    {
        // Add a new permission
        _ = await client.PostAsJsonAsync("/Api/Permission", new
        {
            Name = "Permission 1"
        });
        _ = await client.PostAsJsonAsync("/Api/Permission", new
        {
            Name = "Permission 2"
        });

        await Task.Delay(10000);

        var apiResponse = await client.GetAsync("/Api/Permission");
        var jsonApiResponse = await apiResponse.Content.ReadAsStringAsync();

        var allPermissions = JsonSerializer.Deserialize<List<PermissionRecord>>(jsonApiResponse);
        Assert.NotNull(allPermissions);
        Assert.Equal(2, allPermissions?.Count);

        var elasticResponse = await elasticClient
            .SearchAsync<PermissionRecord>(s =>
                s.MatchAll()
            );
        var elasticPermissions = elasticResponse.Documents
            .ToList();

        Assert.True(elasticResponse.IsValid);
        Assert.Equal(2, elasticPermissions.Count);
        Assert.Equal(allPermissions, elasticPermissions);
    }
}
