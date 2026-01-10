using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ImaginariumBackend.DTOs;
using ImaginariumBackend.Services;

namespace ImaginariumBackend.Controllers;

[ApiController]
[Route("api/accounts")]
[EnableCors("AllowAngular")]
public class AccountController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IUserService userService, ILogger<AccountController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            var result = await _userService.RegisterAsync(registerDto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas rejestracji użytkownika");
            return StatusCode(500, new { message = "Wystąpił błąd podczas rejestracji." });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var result = await _userService.LoginAsync(loginDto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas logowania użytkownika");
            return StatusCode(500, new { message = "Wystąpił błąd podczas logowania." });
        }
    }
}
