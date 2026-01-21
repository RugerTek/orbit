using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrbitOS.Application.Interfaces;

namespace OrbitOS.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? AzureAdObjectId => User?.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
        ?? User?.FindFirstValue("oid");

    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue("email")
        ?? User?.FindFirstValue("preferred_username");

    public string? DisplayName => User?.FindFirstValue("name")
        ?? User?.FindFirstValue(ClaimTypes.Name);

    public string? TenantId => User?.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid")
        ?? User?.FindFirstValue("tid");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
