using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrbitOS.Application.Interfaces;
using OrbitOS.Domain.Entities;
using OrbitOS.Infrastructure.Data;

namespace OrbitOS.Api.Controllers;

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class LoginResponse
{
    public required string Token { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class GoogleLoginRequest
{
    public required string Credential { get; set; }
}

public class GoogleCodeRequest
{
    public required string Code { get; set; }
    public required string RedirectUri { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthController> _logger;
    private readonly OrbitOSDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthController(
        ICurrentUserService currentUserService,
        ILogger<AuthController> logger,
        OrbitOSDbContext dbContext,
        IConfiguration configuration)
    {
        _currentUserService = currentUserService;
        _logger = logger;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var token = GenerateJwtToken(user.Id.ToString(), user.Email, user.DisplayName);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        return Ok(new LoginResponse
        {
            Token = token,
            Email = user.Email,
            DisplayName = user.DisplayName,
            ExpiresAt = expiresAt
        });
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            // Verify the Google token
            var googleUser = await VerifyGoogleToken(request.Credential);
            if (googleUser == null)
            {
                return Unauthorized(new { Message = "Invalid Google token" });
            }

            return await ProcessGoogleUser(googleUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google login failed");
            return Unauthorized(new { Message = "Google authentication failed" });
        }
    }

    [HttpPost("google-code")]
    public async Task<IActionResult> GoogleLoginWithCode([FromBody] GoogleCodeRequest request)
    {
        try
        {
            // Exchange authorization code for tokens
            var googleUser = await ExchangeCodeForUserInfo(request.Code, request.RedirectUri);
            if (googleUser == null)
            {
                return Unauthorized(new { Message = "Failed to exchange authorization code" });
            }

            return await ProcessGoogleUser(googleUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google code exchange failed");
            return Unauthorized(new { Message = "Google authentication failed" });
        }
    }

    private async Task<IActionResult> ProcessGoogleUser(GoogleUserInfo googleUser)
    {
        // Find or create user
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleUser.Sub || u.Email.ToLower() == googleUser.Email.ToLower());

        if (user == null)
        {
            // Create new user
            user = new User
            {
                GoogleId = googleUser.Sub,
                Email = googleUser.Email,
                DisplayName = googleUser.Name,
                FirstName = googleUser.GivenName,
                LastName = googleUser.FamilyName,
                AvatarUrl = googleUser.Picture
            };
            _dbContext.Users.Add(user);
        }
        else if (string.IsNullOrEmpty(user.GoogleId))
        {
            // Link Google account to existing user
            user.GoogleId = googleUser.Sub;
            if (string.IsNullOrEmpty(user.AvatarUrl))
            {
                user.AvatarUrl = googleUser.Picture;
            }
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var token = GenerateJwtToken(user.Id.ToString(), user.Email, user.DisplayName);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        return Ok(new LoginResponse
        {
            Token = token,
            Email = user.Email,
            DisplayName = user.DisplayName,
            ExpiresAt = expiresAt
        });
    }

    private async Task<GoogleUserInfo?> ExchangeCodeForUserInfo(string code, string redirectUri)
    {
        var googleClientId = _configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId is not configured. Set the Google:ClientId configuration value.");
        var googleClientSecret = _configuration["Google:ClientSecret"]
            ?? throw new InvalidOperationException("Google:ClientSecret is not configured. Set the Google:ClientSecret configuration value.");

        using var httpClient = new HttpClient();

        // Exchange code for tokens
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = googleClientId,
            ["client_secret"] = googleClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogError("Token exchange failed: {Error}", errorContent);
            return null;
        }

        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        var tokens = JsonSerializer.Deserialize<GoogleTokenResponse>(tokenContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (tokens?.AccessToken == null)
        {
            return null;
        }

        // Get user info using access token
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var userInfoResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
        if (!userInfoResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<GoogleUserInfoV2>(userInfoContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (userInfo == null)
        {
            return null;
        }

        return new GoogleUserInfo
        {
            Sub = userInfo.Id,
            Email = userInfo.Email,
            Name = userInfo.Name,
            Picture = userInfo.Picture,
            GivenName = userInfo.GivenName,
            FamilyName = userInfo.FamilyName
        };
    }

    private class GoogleTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("id_token")]
        public string? IdToken { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    private class GoogleUserInfoV2
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("email")]
        public string Email { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("picture")]
        public string Picture { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("given_name")]
        public string GivenName { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("family_name")]
        public string FamilyName { get; set; } = "";
    }

    private async Task<GoogleUserInfo?> VerifyGoogleToken(string credential)
    {
        var googleClientId = _configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId is not configured. Set the Google:ClientId configuration value.");

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={credential}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var tokenInfo = JsonSerializer.Deserialize<GoogleUserInfo>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Verify the token is for our app
        if (tokenInfo?.Aud != googleClientId)
        {
            return null;
        }

        return tokenInfo;
    }

    private class GoogleUserInfo
    {
        public string Sub { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string Picture { get; set; } = "";
        public string Aud { get; set; } = "";
        public string GivenName { get; set; } = "";
        public string FamilyName { get; set; } = "";
    }

    private string GenerateJwtToken(string userId, string email, string displayName)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured. Set a secure JWT signing key (minimum 32 characters).");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("name", displayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "OrbitOS",
            audience: _configuration["Jwt:Audience"] ?? "OrbitOS",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        var hashBytes = Convert.FromBase64String(passwordHash);
        var salt = hashBytes[..16];
        var storedHash = hashBytes[16..];

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var computedHash = pbkdf2.GetBytes(32);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            ObjectId = _currentUserService.AzureAdObjectId,
            Email = _currentUserService.Email,
            DisplayName = _currentUserService.DisplayName,
            TenantId = _currentUserService.TenantId
        });
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { Message = "OrbitOS API is running", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("ping-auth")]
    [Authorize]
    public IActionResult PingAuthenticated()
    {
        return Ok(new
        {
            Message = "Authenticated successfully",
            User = _currentUserService.Email,
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("my-organizations")]
    [Authorize]
    public async Task<IActionResult> GetMyOrganizations()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized();
        }

        // Get user by email
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == _currentUserService.Email.ToLower());

        if (user == null)
        {
            return Ok(new List<UserOrganizationDto>());
        }

        var organizations = await _dbContext.OrganizationMemberships
            .Include(m => m.Organization)
            .Where(m => m.UserId == user.Id && m.DeletedAt == null)
            .Select(m => new UserOrganizationDto
            {
                Id = m.Organization.Id,
                Name = m.Organization.Name,
                Slug = m.Organization.Slug,
                Description = m.Organization.Description,
                LogoUrl = m.Organization.LogoUrl,
                Role = m.Role,
                CreatedAt = m.Organization.CreatedAt
            })
            .OrderBy(o => o.Name)
            .ToListAsync();

        return Ok(organizations);
    }

    [HttpPost("organizations")]
    [Authorize]
    public async Task<IActionResult> CreateOrganization([FromBody] UserCreateOrganizationRequest request)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized();
        }

        // Get user by email
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == _currentUserService.Email.ToLower());

        if (user == null)
        {
            return Unauthorized(new { Message = "User not found" });
        }

        // Generate slug from name
        var slug = GenerateSlug(request.Name);

        // Check if slug is unique
        var slugExists = await _dbContext.Organizations.AnyAsync(o => o.Slug == slug && o.DeletedAt == null);
        if (slugExists)
        {
            // Append random suffix
            slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";
        }

        var organization = new Organization
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            LogoUrl = request.LogoUrl
        };

        _dbContext.Organizations.Add(organization);

        // Add current user as owner
        var membership = new OrganizationMembership
        {
            UserId = user.Id,
            OrganizationId = organization.Id,
            Role = MembershipRole.Owner
        };

        _dbContext.OrganizationMemberships.Add(membership);

        await _dbContext.SaveChangesAsync();

        return Ok(new UserOrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Slug = organization.Slug,
            Description = organization.Description,
            LogoUrl = organization.LogoUrl,
            Role = MembershipRole.Owner,
            CreatedAt = organization.CreatedAt
        });
    }

    private static string GenerateSlug(string name)
    {
        // Simple slug generation - lowercase, replace spaces with dashes, remove special chars
        return System.Text.RegularExpressions.Regex.Replace(
            name.ToLower().Replace(" ", "-"),
            "[^a-z0-9-]",
            ""
        );
    }

    #region User Profile Endpoints

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == _currentUserService.Email.ToLower());

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            HasPassword = !string.IsNullOrEmpty(user.PasswordHash),
            HasGoogleId = !string.IsNullOrEmpty(user.GoogleId),
            HasAzureAdId = !string.IsNullOrEmpty(user.AzureAdObjectId),
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized();
        }

        // Validate display name
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequest(new { Message = "Display name is required" });
        }

        if (request.DisplayName.Length < 2 || request.DisplayName.Length > 100)
        {
            return BadRequest(new { Message = "Display name must be between 2 and 100 characters" });
        }

        // Validate optional fields
        if (request.FirstName != null && request.FirstName.Length > 50)
        {
            return BadRequest(new { Message = "First name must be 50 characters or less" });
        }

        if (request.LastName != null && request.LastName.Length > 50)
        {
            return BadRequest(new { Message = "Last name must be 50 characters or less" });
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == _currentUserService.Email.ToLower());

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        user.DisplayName = request.DisplayName.Trim();
        user.FirstName = request.FirstName?.Trim();
        user.LastName = request.LastName?.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated their profile", user.Id);

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            HasPassword = !string.IsNullOrEmpty(user.PasswordHash),
            HasGoogleId = !string.IsNullOrEmpty(user.GoogleId),
            HasAzureAdId = !string.IsNullOrEmpty(user.AzureAdObjectId),
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized();
        }

        // Validate passwords match
        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new { Message = "New password and confirmation do not match" });
        }

        // Validate password length
        if (request.NewPassword.Length < 8)
        {
            return BadRequest(new { Message = "Password must be at least 8 characters" });
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == _currentUserService.Email.ToLower());

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        // User must have a password set to change it
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return BadRequest(new { Message = "Cannot change password for accounts without password authentication" });
        }

        // Verify current password
        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { Message = "Current password is incorrect" });
        }

        // Hash and save new password
        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} changed their password", user.Id);

        return Ok(new { Message = "Password changed successfully" });
    }

    [HttpPost("link-google")]
    [Authorize]
    public async Task<IActionResult> LinkGoogleAccount([FromBody] LinkGoogleRequest request)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == _currentUserService.Email.ToLower());

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        if (!string.IsNullOrEmpty(user.GoogleId))
        {
            return BadRequest(new { Message = "Google account is already linked" });
        }

        // Verify the Google token
        var googleUser = await VerifyGoogleToken(request.Credential);
        if (googleUser == null)
        {
            return BadRequest(new { Message = "Invalid Google token" });
        }

        // Check if Google account is already linked to another user
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleUser.Sub && u.Id != user.Id);

        if (existingUser != null)
        {
            return BadRequest(new { Message = "This Google account is already linked to another user" });
        }

        // Link Google account
        user.GoogleId = googleUser.Sub;
        if (string.IsNullOrEmpty(user.AvatarUrl))
        {
            user.AvatarUrl = googleUser.Picture;
        }
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} linked their Google account", user.Id);

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            HasPassword = !string.IsNullOrEmpty(user.PasswordHash),
            HasGoogleId = !string.IsNullOrEmpty(user.GoogleId),
            HasAzureAdId = !string.IsNullOrEmpty(user.AzureAdObjectId),
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }

    #endregion
}

public class UserOrganizationDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public MembershipRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserCreateOrganizationRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
}

#region User Profile DTOs

public class UserProfileDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool HasPassword { get; set; }
    public bool HasGoogleId { get; set; }
    public bool HasAzureAdId { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateProfileRequest
{
    public required string DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmPassword { get; set; }
}

public class LinkGoogleRequest
{
    public required string Credential { get; set; }
}

#endregion
