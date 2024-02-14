using System.Text.Json;
using ApiService.Domain.Messages;
using ApiService.IntegrationTests.Setup;
using Confluent.Kafka;

namespace ApiService.IntegrationTests;

public class IntegrationTestOld
{
    [Theory]
    [TestControllerSetup]
    public async Task PushPermissionToKafka(HttpClient client, IConsumer<Null, string> consumer, PermissionTopicMessage permission)
    {
        // Act
        var response = await client.GetAsync("/WeatherForecast");

        // Assert
        response.EnsureSuccessStatusCode();

        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(5));
        var consumedPermission = JsonSerializer.Deserialize<PermissionTopicMessage>(consumeResult.Message.Value);

        //Assert.Equal(
        //    permission,
        //    consumedPermission
        //);

        Assert.Equal(
            new PermissionRecord(1, "Permission 1"),
            consumedPermission.Permission
        );
    }
}
