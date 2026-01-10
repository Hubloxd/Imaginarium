using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ImaginariumBackend.DTOs;
using ImaginariumBackend.Services;

namespace ImaginariumBackend.Controllers;

[ApiController]
[Route("api/shares")]
[Authorize]
[EnableCors("AllowAngular")]
public class ShareController : ControllerBase
{
    private readonly IShareService _shareService;
    private readonly ILogger<ShareController> _logger;

    public ShareController(IShareService shareService, ILogger<ShareController> logger)
    {
        _shareService = shareService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ShareResponseDto>> CreateShare([FromBody] CreateShareDto createDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _shareService.CreateShareAsync(userId.Value, createDto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia udostępnienia");
            return StatusCode(500, new { message = "Wystąpił błąd podczas tworzenia udostępnienia." });
        }
    }

    [HttpGet("token/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<ShareResponseDto>> GetShareByToken(string token)
    {
        try
        {
            var share = await _shareService.GetShareByTokenAsync(token);
            if (share == null)
                return NotFound();

            return Ok(share);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania udostępnienia");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania udostępnienia." });
        }
    }

    [HttpGet("validate/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> ValidateAccess(string token)
    {
        try
        {
            var userId = GetCurrentUserId(); // Może być null dla anonimowych
            var hasAccess = await _shareService.ValidateAccessAsync(token, userId);
            return Ok(hasAccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas walidacji dostępu");
            return StatusCode(500, new { message = "Wystąpił błąd podczas walidacji dostępu." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> RevokeShare(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _shareService.RevokeShareAsync(id, userId.Value);
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas cofania udostępnienia");
            return StatusCode(500, new { message = "Wystąpił błąd podczas cofania udostępnienia." });
        }
    }

    [HttpPost("group")]
    public async Task<ActionResult<ShareResponseDto>> ShareWithGroup([FromBody] CreateShareDto createDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _shareService.ShareWithGroupAsync(userId.Value, createDto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas udostępniania grupie");
            return StatusCode(500, new { message = "Wystąpił błąd podczas udostępniania grupie." });
        }
    }

    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<List<ShareResponseDto>>> GetGroupShares(Guid groupId)
    {
        try
        {
            var shares = await _shareService.GetGroupSharesAsync(groupId);
            return Ok(shares);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania udostępnień grupy");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania udostępnień grupy." });
        }
    }

    [HttpGet("for-me")]
    public async Task<ActionResult<List<ShareResponseDto>>> GetSharesForMe()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var shares = await _shareService.GetSharesForUserAsync(userId.Value);
            return Ok(shares);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania udostępnień dla użytkownika");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania udostępnień." });
        }
    }

    [HttpGet("by-me")]
    public async Task<ActionResult<List<ShareResponseDto>>> GetSharesByMe()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var shares = await _shareService.GetSharesByUserAsync(userId.Value);
            return Ok(shares);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania udostępnień utworzonych przez użytkownika");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania udostępnień." });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }
}
