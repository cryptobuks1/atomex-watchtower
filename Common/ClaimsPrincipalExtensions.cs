using System.Security.Claims;

namespace Atomex.Common
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetName(this ClaimsPrincipal principal) =>
            principal.FindFirst(ClaimTypes.Name)?.Value;
    }
}