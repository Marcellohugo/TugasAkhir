using Cashflowpoly.Api.Models;

namespace Cashflowpoly.Api.Controllers;

internal static class ApiErrorHelper
{
    internal static ErrorResponse BuildError(HttpContext httpContext, string code, string message, params ErrorDetail[] details)
    {
        return new ErrorResponse(code, message, details.ToList(), httpContext.TraceIdentifier);
    }
}
