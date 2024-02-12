using ApiService.Domain.Entities;
using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Infrastructure.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApiService.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IKafkaProducer _producer;
    private readonly IOptions<KafkaOptions> _kafkaOptions;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IKafkaProducer producer, IOptions<KafkaOptions> kafkaOptions)
    {
        _logger = logger;
        _producer = producer;
        _kafkaOptions = kafkaOptions;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        await _producer.Produce(
            _kafkaOptions.Value.PermissionsTopicName,
            new PermissionTopicMessage(
                Guid.NewGuid(),
                NameOperationEnum.get,
                new PermissionRecord(1, "Permission 1")
            )
        );

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
}

