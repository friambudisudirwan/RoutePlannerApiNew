using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace RoutePlanner_Api.Dtos;

public class IntegrateCarErrorItem
{
    [JsonPropertyName("carid")]
    public string CarId { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class IntegrateRunsheetsResult
{
    public List<long> DoIds { get; } = [];
    public List<string> SuccessCarIds { get; } = [];
    public List<IntegrateCarErrorItem> Errors { get; } = [];

    public (int statusCode, object body) ToHttpResponse()
    {
        var fail = Errors.Count;
        var success = SuccessCarIds.Count;
        var error_data = Errors.Select(x => new { carid = x.CarId, message = x.Message });

        if (success == 0 && fail > 0)
        {
            return (StatusCodes.Status422UnprocessableEntity, new
            {
                message = fail == 1
                    ? Errors[0].Message
                    : $"{fail} vehicle{(fail > 1 ? "s" : "")} failed to integrate.",
                data = Array.Empty<object>(),
                error_data
            });
        }

        var message = fail == 0
            ? "Runsheets berhasil diintegrasikan ke TMS EasyGO."
            : $"{success} vehicle{(success > 1 ? "s" : "")} integrated successfully, with {fail} vehicle{(fail > 1 ? "s" : "")} failed.";

        return (StatusCodes.Status201Created, new
        {
            message,
            data = DoIds.Select(x => new { do_id = x }),
            error_data
        });
    }
}
