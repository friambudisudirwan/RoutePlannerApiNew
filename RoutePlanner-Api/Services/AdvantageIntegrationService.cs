using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using RestSharp;

namespace RoutePlanner_Api.Services;

public sealed class AdvantageIntegrationService
(
    IConfiguration config,
    RestClient restClient
)
{
    private readonly string _baseUrl = config.GetSection("AdvantageSCMIntegrationConfig")["BaseUrl"] ?? throw new InvalidOperationException("AdvantageSCMIntegrationConfig BaseUrl not found");
    private readonly string _private_key = ResolvePrivateKey(config);
    private readonly RestClient _restClient = restClient;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Include,
        DefaultValueHandling = DefaultValueHandling.Include
    };
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
