using System;
using Newtonsoft.Json;

namespace RoutePlanner_Api.Extensions;

public static class JsonExtensions
{
    public static bool TryParseJson<T>(this string json, out T result)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            result = default;
            return false;
        }

        try
        {
            result = JsonConvert.DeserializeObject<T>(json);
            return result != null;
        }
        catch (JsonException)
        {
            // Catches parsing, format, and reading errors safely
            result = default;
            return false;
        }
    }
}
