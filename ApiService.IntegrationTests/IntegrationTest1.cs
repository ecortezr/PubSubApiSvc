using System.Text.Json;
using ApiService.Domain.Messages;
using Confluent.Kafka;

namespace ApiService.IntegrationTests;

public class IntegrationTest1
{
    [Theory]
    [TestControllerSetup]
    public async Task PushOrderToKafka(HttpClient client, IConsumer<Null, string> consumer, PermissionTopicMessage permission)
    {
        // Act
        var response = await client.GetAsync("/WeatherForecast");

        // Assert
        response.EnsureSuccessStatusCode();

        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(5));
        var consumedPermission = JsonSerializer.Deserialize<PermissionTopicMessage>(consumeResult.Message.Value);

        Assert.Equal(
            permission,
            consumedPermission
        );
    }
}
