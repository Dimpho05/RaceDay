using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Api.Filters;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IPasswordService _passwordService;

    public AuthController(RaceDayDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    /// <summary>
    /// Registers a new Organiser or Participant account. The password is hashed before it is stored.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim().ToLower();

        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = _passwordService.Hash(request.Password),
            Role = request.Role,
            PhoneNumber = request.PhoneNumber
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var response = new UserResponse
        {
            UserID = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }


    /// <summary>
    /// Logs a user in and starts a session that stores their user ID and role.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such email" and "wrong password" so attackers
        // can't discover which emails are registered
        if (user == null || !_passwordService.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // The server remembers who this is for all later requests
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(new UserResponse
        {
            UserID = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber
        });
    }

    /// <summary>
    /// Logs the current user out by clearing their session.
    /// </summary>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out successfully." });
    }
}