﻿using System.Text.Json;
using ApiService.Domain.Repositories;
using ApiService.Infrastructure.Utils;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace ApiService.Infrastructure.Storage;

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<Null, string> kafkaProducer;

    private int disposed;

    public KafkaProducer(IOptions<KafkaOptions> kafkaOptions)
    {
        var config = new ProducerConfig { BootstrapServers = kafkaOptions.Value.BootstrapServers };
        kafkaProducer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task Produce<TMessage>(string topic, TMessage message)
    {
        await kafkaProducer.ProduceAsync(topic, new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(message)
        });
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) == 1) return;
        kafkaProducer?.Flush();
        kafkaProducer?.Dispose();
    }
}