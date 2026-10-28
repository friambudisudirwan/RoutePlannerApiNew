namespace RoutePlanner_Api.Dtos;

public class ResponseAdvantageIntegration
{
    public ResponseAdvantageIntegrationBody? data { get; set; }
}

public class ResponseAdvantageIntegrationBody
{
    public int statusCode { get; set; }
    public string? message { get; set; }
}
