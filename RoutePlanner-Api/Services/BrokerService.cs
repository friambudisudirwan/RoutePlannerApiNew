using System.Text;
using RabbitMQ.Client;

namespace RoutePlanner_Api.Services;

public interface IBrokerService
{
    Task PublishMessage(string exchange, string routing_key, string message);

}
public class BrokerService
(
    IConfiguration config,
    ILogger<BrokerService> logger
) : IBrokerService, IHostedService, IDisposable
{
    private readonly ILogger<BrokerService> _logger = logger;
    private readonly dynamic _brokerConfig = config.GetSection("RabbitMQConfig");
    private IConnection? _conn;
    private IChannel? _channel;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _brokerConfig["HostName"],
            VirtualHost = _brokerConfig["VirtualHostName"],
            UserName = _brokerConfig["UserName"],
            Password = _brokerConfig["Password"]
        };

        _conn = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _conn.CreateChannelAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Broker Service connected to Message Broker");
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

        _logger.LogInformation("message published with payload : {payload}", message);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
            await _channel.DisposeAsync();

        if (_conn != null)
            await _conn.DisposeAsync();

        _logger.LogInformation("Broker and SQL connection dispossed.");
    }

    public void Dispose() { }
}
