using System.Text.Json.Serialization;
using RoutePlanner_Api.Models;

namespace RoutePlanner_Api.Dtos;

/// <summary>Generic message-only response.</summary>
public class MessageResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>Login success response.</summary>
public class LoginResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>Run ID item returned after create runsheets.</summary>
public class RunIdItem
{
    [JsonPropertyName("RunID")]
    public string RunID { get; set; } = string.Empty;
}

/// <summary>Create runsheets success response.</summary>
public class CreateRunsheetsResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<RunIdItem> Data { get; set; } = [];
}

/// <summary>DO ID item returned after integrate runsheets.</summary>
public class DoIdItem
{
    [JsonPropertyName("do_id")]
    public string DoId { get; set; } = string.Empty;
}

/// <summary>Integrate runsheets success response.</summary>
public class IntegrateRunsheetsResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<DoIdItem> Data { get; set; } = [];
}

/// <summary>Prambanan SO validation error (duplicate SO / invalid coordinates).</summary>
public class PrambananValidationErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("duplicate_so")]
    public List<NotValidDuplicateSo> DuplicateSo { get; set; } = [];

    [JsonPropertyName("not_valid_lon_lat")]
    public List<NotValidLonLatSo> NotValidLonLat { get; set; } = [];
}

/// <summary>Update PS not-found error.</summary>
public class UpdatePSNotFoundResponse
{
    [JsonPropertyName("list_not_found_so")]
    public List<ParamUpdatePSItem> ListNotFoundSo { get; set; } = [];
}

/// <summary>Update PS unexpected error with trace id.</summary>
public class UpdatePSErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("trace_id")]
    public string TraceId { get; set; } = string.Empty;
}
