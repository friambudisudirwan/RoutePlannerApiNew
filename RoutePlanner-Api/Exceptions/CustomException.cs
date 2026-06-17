using System;

namespace RoutePlanner_Api.Exceptions;

public class CustomException(string message, int statusCode) : Exception(message)
{
    public int status_code = statusCode;
}
