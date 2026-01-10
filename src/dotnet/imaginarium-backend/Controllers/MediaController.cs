using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ImaginariumBackend.DTOs;
using ImaginariumBackend.Services;

namespace ImaginariumBackend.Controllers;

[ApiController]
[Route("api/media")]
[Authorize]
[EnableCors("AllowAngular")]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(IMediaService mediaService, ILogger<MediaController> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<MediaResponseDto>>> GetUserMedia()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var media = await _mediaService.GetUserMediaAsync(userId.Value);
            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania mediów");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania mediów." });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MediaResponseDto>> GetMedia(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var media = await _mediaService.GetMediaByIdAsync(id, userId.Value);
            if (media == null)
                return NotFound();

            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania media");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania media." });
        }
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(4294967296)]
    [RequestFormLimits(MultipartBodyLengthLimit = 4294967296)]
    public async Task<ActionResult<MediaResponseDto>> UpdateMedia(Guid id, IFormFile file)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Plik jest wymagany." });

            var media = await _mediaService.UpdateMediaAsync(id, userId.Value, file);
            if (media == null)
                return NotFound();

            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji media");
            return StatusCode(500, new { message = "Wystąpił błąd podczas aktualizacji media." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMedia(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var deleted = await _mediaService.DeleteMediaAsync(id, userId.Value);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania media");
            return StatusCode(500, new { message = "Wystąpił błąd podczas usuwania media." });
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
