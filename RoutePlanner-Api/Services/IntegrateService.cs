using System;
using Newtonsoft.Json;
using RestSharp;
using RoutePlanner_Api.Dtos;
using RoutePlanner_Api.Exceptions;

namespace RoutePlanner_Api.Services;

public class IntegrateService
(
    IConfiguration config,
    ILogger<IntegrateService> logger
)
{
    private readonly ILogger<IntegrateService> _logger = logger;
    private readonly string _vtsApiUrl = config.GetSection("Configs")["VtsApiUrl"] ?? throw new ArgumentNullException("Vts Api Url is empty");

    public async Task<long> AddOrUpdateDOV1ByGeoCode
    (
        string token_h2h,
        object payload,
        CancellationToken cancellationToken
    )
    {
        // **hit vts api create do by code
        var client = new RestClient(_vtsApiUrl);
        var request = new RestRequest("/api/do/AddOrUpdateDOV1ByGeoCode", Method.Post);

        // Header Token
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Token", token_h2h);

        var request_body = JsonConvert.SerializeObject(payload);
        request.AddParameter(
            "application/json",
            request_body,
            ParameterType.RequestBody
        );

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(response.ErrorMessage ?? "Internal Server Error");
            throw new CustomException(response.ErrorMessage ?? "Internal Server Error", (int)response.StatusCode);
        }
        if (string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogError("No content while integrating to TMS EasyGo. Internal Server Error");
            throw new CustomException("No content while integrating to TMS EasyGo. Internal Server Error", StatusCodes.Status500InternalServerError);
        }

        var responseData = JsonConvert.DeserializeObject<VtsApiResponseBase<DoIdData>>(response.Content ?? "");
        if (responseData is null)
        {
            _logger.LogError("Failed when integrating to TMS EasyGO");
            throw new CustomException("Failed when integrating to TMS EasyGO", StatusCodes.Status500InternalServerError);
        }
        if (responseData.ResponseCode != 1)
        {
            _logger.LogError("Integration Failed to TMS EasyGO, with Message: {message}", responseData.ResponseMessage);
            throw new CustomException(responseData.ResponseMessage, StatusCodes.Status500InternalServerError);
        }

        var do_id = responseData?.Data?.do_id ?? 0;
        if (do_id == 0)
        {
            _logger.LogError("Failed when getting do_id from TMS EasyGo");
            throw new CustomException("Failed when getting do_id from TMS EasyGo", StatusCodes.Status500InternalServerError);
        }

        return do_id;
    }
}
