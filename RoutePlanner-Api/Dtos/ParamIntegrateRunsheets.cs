namespace RoutePlanner_Api.Dtos;

/// <summary>Request body for integrating runsheets into TMS EasyGO.</summary>
public record class ParamIntegrateRunsheets
{
    /// <summary>List of runid / carid pairs to integrate.</summary>
    public required List<ParamTrxRoute> data { get; set; }
}
