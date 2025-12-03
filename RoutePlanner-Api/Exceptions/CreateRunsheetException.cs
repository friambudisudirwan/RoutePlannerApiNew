using System;

namespace RoutePlanner_Api.Exceptions;

public class CreateRunsheetException(string message) : Exception(message)
{
}
