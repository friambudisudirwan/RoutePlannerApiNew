using System;

namespace RoutePlanner_Api.Dtos;

public class ParamAdvantageIntegrationData
{
    public required ParamAdvantageIntegrationPool pool { get; set; }
    public required List<ParamAdvantageIntegrationTripSuccess> success { get; set; }
    public required List<ParamAdvantageIntegrationTripUnsuccess> unsuccess { get; set; }
}
