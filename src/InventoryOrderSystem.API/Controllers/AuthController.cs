using System.Text.RegularExpressions;
using InventoryOrderSystem.API.DTOs.Auth;
using InventoryOrderSystem.API.Services;
using InventoryOrderSystem.Domain.Entities;
using InventoryOrderSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private const string AdminRole = "Admin";

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new tenant (organization) along with its first Admin user.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterTenantRequest request)
    {
        var normalizedEmail = _userManager.NormalizeEmail(request.AdminEmail);

        // ApplicationUser has a tenant-scoped query filter, and at this point there is
        // no authenticated tenant context yet, so we must bypass it to check uniqueness
        // globally across all tenants.
        var emailExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail);

        if (emailExists)
        {
            _logger.LogWarning("Registration rejected: email already in use");
            return Conflict(new { message = "An account with this email already exists." });
        }

        var slug = await GenerateUniqueSlugAsync(request.CompanyName);

        var tenant = new Tenant { Name = request.CompanyName, Slug = slug, IsActive = true };
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = request.AdminEmail,
            Email = request.AdminEmail,
            FullName = request.AdminFullName,
            TenantId = tenant.Id,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            _dbContext.Tenants.Remove(tenant);
            await _dbContext.SaveChangesAsync();
            return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });
        }

        if (!await _roleManager.RoleExistsAsync(AdminRole))
            await _roleManager.CreateAsync(new IdentityRole<int>(AdminRole));

        await _userManager.AddToRoleAsync(user, AdminRole);

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = _jwtTokenService.GenerateToken(user, roles);

        _logger.LogInformation(
            "New tenant registered: {TenantName} (Id {TenantId}, slug {Slug}), admin user {UserId}",
            tenant.Name, tenant.Id, tenant.Slug, user.Id);

        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Roles = roles
        });
    }

    /// <summary>
    /// Authenticates a user and returns a JWT carrying their tenantId claim.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var normalizedEmail = _userManager.NormalizeEmail(request.Email);

        // Bypass the tenant filter: we don't know the caller's tenant until we find them.
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        if (user is null || !user.IsActive || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            _logger.LogWarning("Login failed for email {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId);
        if (tenant is null || !tenant.IsActive)
        {
            _logger.LogWarning("Login rejected for user {UserId}: tenant inactive", user.Id);
            return Unauthorized(new { message = "This account's organization is inactive." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = _jwtTokenService.GenerateToken(user, roles);

        _logger.LogInformation("User {UserId} logged in for tenant {TenantId}", user.Id, tenant.Id);

        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Roles = roles
        });
    }

    private async Task<string> GenerateUniqueSlugAsync(string companyName)
    {
        var baseSlug = Regex.Replace(companyName.Trim().ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        baseSlug = Regex.Replace(baseSlug, @"\s+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "org";

        var slug = baseSlug;
        var suffix = 1;
        while (await _dbContext.Tenants.AnyAsync(t => t.Slug == slug))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }
}
