using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using RestSharp;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Exceptions;
using RoutePlanner_Api.Extensions;

namespace RoutePlanner_Api.Services;

public sealed class AdvantageIntegrationService
(
    IConfiguration config,
    ILogger<AdvantageIntegrationService> logger
)
{
    private readonly ILogger<AdvantageIntegrationService> _logger = logger;
    private readonly string _baseUrl = config.GetSection("AdvantageSCMIntegrationConfig")["BaseUrl"] ?? throw new InvalidOperationException("AdvantageSCMIntegrationConfig BaseUrl not found");
    private readonly string _endpoint = NormalizeEndpoint(config.GetSection("AdvantageSCMIntegrationConfig")["Endpoint"] ?? throw new InvalidOperationException("AdvantageSCMIntegrationConfig Endpoint not found"));
    private readonly string _api_key = config.GetSection("AdvantageSCMIntegrationConfig")["ApiKey"] ?? throw new InvalidOperationException("AdvantageSCMIntegrationConfig ApiKey not found");
    private readonly string _private_key = ResolvePrivateKey(config);

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Include,
        DefaultValueHandling = DefaultValueHandling.Include
    };

    public async Task<(int httpStatus, string message)> SendUpdateAsync(ParamAdvantageIntegration payload, CancellationToken cancellationToken)
    {
        var json_body = JsonConvert.SerializeObject(payload, JsonSettings);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = Sign(_private_key, timestamp, "PUT", _endpoint, json_body);

        var client = new RestClient(_baseUrl);
        var request = new RestRequest(_endpoint, Method.Put);
        request.AddStringBody(json_body, ContentType.Json);
        request.AddHeader("X-API-Key", _api_key);
        request.AddHeader("X-Signature", signature);
        request.AddHeader("X-Timestamp", timestamp);
        request.AddHeader("Content-Type", "application/json");

        var response = await client.ExecuteAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var status = (int)response.StatusCode;
            if ((response.Content ?? "").TryParseJson(out ResponseAdvantageIntegrationError content)
                && !string.IsNullOrWhiteSpace(content.message))
            {
                _logger.LogWarning("Advantage callback failed ({Status}): {Message}", status, content.message);
                throw new CustomException(content.message, status == 0 ? StatusCodes.Status500InternalServerError : status);
            }

            _logger.LogError("Advantage callback failed ({Status}): {Content}", status, response.Content);
            throw new CustomException(
                response.ErrorMessage ?? "Advantage SCM Integration Failed. Internal server error",
                status == 0 ? StatusCodes.Status500InternalServerError : status);
        }

        var message = "Success";
        if ((response.Content ?? "").TryParseJson(out ResponseAdvantageIntegration res)
            && !string.IsNullOrWhiteSpace(res.data?.message))
        {
            message = res.data.message;
        }

        return ((int)response.StatusCode, message);
    }

    private static string NormalizeEndpoint(string endpoint)
        => endpoint.StartsWith('/') ? endpoint : $"/{endpoint}";

    private static string ResolvePrivateKey(IConfiguration config)
    {
        var path = config.GetSection("AdvantageSCMIntegrationConfig")["PrivateKeyPath"];

        if (!string.IsNullOrWhiteSpace(path))
        {
            var key = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("AdvantageSCMIntegrationConfig private key path is not configured. Set AdvantageSCMIntegrationConfig:PrivateKeyPath or EasyGo:PrivateKeyPath.");

            return key;
        }

        throw new InvalidOperationException("AdvantageSCMIntegrationConfig private key path is not configured. Set AdvantageSCMIntegrationConfig:PrivateKeyPath or EasyGo:PrivateKeyPath.");
    }

    private static string Sign
    (
        string private_key,
        string timestamp,
        string method,
        string path,
        string json_body
    )
    {
        var message = timestamp + method + path + json_body;
        using var rsa = RSA.Create();
        rsa.ImportFromPem(private_key);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(message));
        var signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }
}
