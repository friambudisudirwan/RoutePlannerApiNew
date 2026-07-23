using System;
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
    IConfiguration config
)
{
    private readonly string _baseUrl = config.GetSection("AdvantageSCMIntegrationConfig")["BaseUrl"] ?? throw new InvalidOperationException("AdvantageSCMIntegrationConfig BaseUrl not found");
    private readonly string _endpoint = config.GetSection("AdvantageSCMIntegrationConfig")["Endpoint"] ?? throw new InvalidOperationException("AdvantageSCMIntegrationConfig Endpoint not found");
    private readonly string _api_key = config.GetSection("AdvantageSCMIntegrationConfig")["ApiKey"] ?? throw new InvalidOperationException("AdvantageSCMIntegrationConfig ApiKey not found");
    private readonly string _private_key = ResolvePrivateKey(config);

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Include,
        DefaultValueHandling = DefaultValueHandling.Include
    };

    public async Task<object> SendUpdateAsync(ParamAdvantageIntegration payload, CancellationToken cancellationToken)
    {
        var json_body = JsonConvert.SerializeObject(payload, JsonSettings);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = Sign(_private_key, timestamp, "PUT", _endpoint, json_body);

        var client = new RestClient(_baseUrl);
        var request = new RestRequest(json_body, Method.Put);
        request.AddStringBody(json_body, ContentType.Json);
        request.AddHeader("X-API-Key", _api_key);
        request.AddHeader("X-Signature", signature);
        request.AddHeader("X-Timestamp", timestamp);

        var response = await client.ExecuteAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if (response.Content.TryParseJson(out ResponseAdvantageIntegrationError content))
            {
                throw new CustomException(content.message, (int)response.StatusCode);
            }
            throw new CustomException("Advantage SCM Integration Failed. Internal server error", StatusCodes.Status500InternalServerError);
        }

        if (response.Content.TryParseJson(out ResponseAdvantageIntegration res))
        {

        }

        return new { };
    }

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
