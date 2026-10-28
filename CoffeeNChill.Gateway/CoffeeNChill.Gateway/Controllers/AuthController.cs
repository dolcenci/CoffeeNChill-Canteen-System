using CoffeeNChill.Gateway.Models;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>
/// Authentication endpoints for user registration and login.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtService _jwtService;

    public AuthController(UserService userService, JwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="request">User registration details including email, password, full name, and role.</param>
    /// <returns>A message confirming successful registration.</returns>
    /// <response code="200">User registered successfully.</response>
    /// <response code="400">Invalid request or user already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Validate role
            var validRoles = new[] { "Admin", "Staff", "Customer" };
            if (!validRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Invalid role. Must be Admin, Staff, or Customer.");

            var user = await _userService.CreateUserAsync(
                request.Email,
                request.Password,
                request.FullName,
                request.Role
            );

            return Ok(new { message = "User registered successfully", email = user.RowKey });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred during registration.", error = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="request">Login credentials (email and password).</param>
    /// <returns>A JWT token with user details.</returns>
    /// <response code="200">Login successful, token returned.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userService.GetUserByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid email or password." });

        if (!_userService.VerifyPassword(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        var response = _jwtService.GenerateToken(user);
        return Ok(response);
    }
}