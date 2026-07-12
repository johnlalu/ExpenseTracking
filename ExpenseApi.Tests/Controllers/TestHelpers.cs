using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpenseApi.Tests.Controllers;

internal static class TestHelpers
{
    internal static ControllerContext MakeControllerContext(string? userId = "user-123")
    {
        var claims = new List<Claim>();
        if (userId != null)
            claims.Add(new Claim("sub", userId));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var httpContext = new DefaultHttpContext
        {
            User = principal,
            TraceIdentifier = "test-trace"
        };

        return new ControllerContext { HttpContext = httpContext };
    }
}
