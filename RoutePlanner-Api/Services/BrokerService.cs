using System.Text;
using RabbitMQ.Client;

namespace RoutePlanner_Api.Services;

public interface IBrokerService
{
    Task PublishMessage(string exchange, string routing_key, string message);

}
public class BrokerService
(
    IConfiguration config
) : IBrokerService, IHostedService, IDisposable
{
    private readonly dynamic _brokerConfig = config.GetSection("RabbitMQConfig");
    private IConnection? _conn;
    private IChannel? _channel;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _brokerConfig["HostName"],
            UserName = _brokerConfig["UserName"],
            Password = _brokerConfig["Password"]
        };

        _conn = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _conn.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    public async Task PublishMessage(string exchange, string routing_key, string message)
    {
        if (_channel is null)
            throw new InvalidOperationException("RabbitMQ channel is not initialized");

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };
        var body = Encoding.UTF8.GetBytes(message);

        await _channel.BasicPublishAsync
        (
            basicProperties: props,
            mandatory: true,
            exchange: exchange,
            routingKey: routing_key,
            body: body
        );
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
            await _channel.DisposeAsync();

        if (_conn != null)
            await _conn.DisposeAsync();
    }

    public void Dispose() { }
}
