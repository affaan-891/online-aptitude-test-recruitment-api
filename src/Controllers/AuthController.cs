using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Webster.AptitudePortal.Api.Data;
using Webster.AptitudePortal.Api.Models.DTOs;

namespace Webster.AptitudePortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AptitudeDbContext _dbContext;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AptitudeDbContext dbContext, IConfiguration config, ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a Manager or Candidate and generates a signed JWT Bearer token
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var identifier = request.Identifier.Trim();

        // 1. Check Manager credentials
        var manager = await _dbContext.Managers
            .FirstOrDefaultAsync(m => m.Email.ToLower() == identifier.ToLower());

        if (manager != null)
        {
            if (BCrypt.Net.BCrypt.Verify(request.Password, manager.PasswordHash))
            {
                var token = GenerateJwtToken(manager.ManagerId, manager.FullName, manager.Email, "Manager");
                _logger.LogInformation("Manager {Email} successfully authenticated.", manager.Email);
                return Ok(token);
            }
        }

        // 2. Check Candidate credentials (by Username or Email)
        var candidate = await _dbContext.Candidates
            .FirstOrDefaultAsync(c => c.Username.ToLower() == identifier.ToLower() || c.Email.ToLower() == identifier.ToLower());

        if (candidate != null)
        {
            if (BCrypt.Net.BCrypt.Verify(request.Password, candidate.PasswordHash))
            {
                var token = GenerateJwtToken(candidate.CandidateId, candidate.FullName, candidate.Email, "Candidate");
                _logger.LogInformation("Candidate {Username} successfully authenticated.", candidate.Username);
                return Ok(token);
            }
        }

        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Invalid Credentials",
            Detail = "The username/email or password provided is incorrect."
        });
    }

    private LoginResponse GenerateJwtToken(int userId, string fullName, string email, string role)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? "WebsterEnterpriseAptitudeSystemSuperSecretKey2026!";
        var issuer = jwtSettings["Issuer"] ?? "Webster.AptitudePortal";
        var audience = jwtSettings["Audience"] ?? "Webster.Applicants";
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "180");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("RoleType", role)
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return new LoginResponse(tokenString, role, userId, fullName, email, expiresAt);
    }
}
