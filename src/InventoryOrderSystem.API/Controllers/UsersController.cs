using System.Security.Claims;
using InventoryOrderSystem.API.DTOs.Users;
using InventoryOrderSystem.API.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderSystem.API.Controllers;

/// <summary>
/// Manages users within the caller's own tenant. Restricted to the Admin role -
/// the first genuinely role-gated endpoint set in the API, as opposed to the
/// "any authenticated user" [Authorize] used elsewhere.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        => Ok(await _userService.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
        => Ok(await _userService.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var user = await _userService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest request)
        => Ok(await _userService.UpdateAsync(id, request, GetCurrentUserId()));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _userService.DeactivateAsync(id, GetCurrentUserId());
        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Missing user id claim on an authenticated request.");
        return int.Parse(claim);
    }
}
