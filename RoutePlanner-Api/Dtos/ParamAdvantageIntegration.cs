namespace RoutePlanner_Api.Dtos;

public class ParamAdvantageIntegration
{
    public int statusCode { get; set; } = 200;
    public string? message { get; set; }
    public required ParamAdvantageIntegrationData data { get; set; }
}
