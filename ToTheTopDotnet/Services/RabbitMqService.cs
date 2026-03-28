using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace LobbyServer.Services;

public class RabbitMqService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string ExchangeName = "lobby_events";

    private RabbitMqService(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<RabbitMqService> CreateAsync()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange: ExchangeName,
            type: ExchangeType.Topic, // allows for pattern matching of routing keys
            durable: true // survives broker restart
            );

        return new RabbitMqService(connection, channel);
    }

    public async Task PublishAsync(string routingKey, object message)
    {
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions(JsonSerializerOptions.Default)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body
        );
        
        Console.WriteLine($"[RabbitMQ] Published {routingKey}: {json}");
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}