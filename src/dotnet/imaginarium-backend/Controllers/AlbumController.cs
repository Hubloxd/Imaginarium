using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ImaginariumBackend.DTOs;
using ImaginariumBackend.Services;
using System.Linq;

namespace ImaginariumBackend.Controllers;

[ApiController]
[Route("api/albums")]
[Authorize]
[EnableCors("AllowAngular")]
public class AlbumController : ControllerBase
{
    private readonly IAlbumService _albumService;
    private readonly ILogger<AlbumController> _logger;

    public AlbumController(IAlbumService albumService, ILogger<AlbumController> logger)
    {
        _albumService = albumService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<AlbumResponseDto>> CreateAlbum([FromForm] CreateAlbumDto createDto, [FromForm] List<IFormFile> files)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _albumService.CreateAlbumAsync(userId.Value, createDto, files);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia albumu");
            return StatusCode(500, new { message = "Wystąpił błąd podczas tworzenia albumu." });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AlbumDetailResponseDto>> GetAlbum(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var album = await _albumService.GetAlbumDetailByIdAsync(id, userId.Value);
            if (album == null)
                return NotFound();

            return Ok(album);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania albumu");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania albumu." });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<AlbumResponseDto>>> GetUserAlbums()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var albums = await _albumService.GetUserAlbumsAsync(userId.Value);
            return Ok(albums);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania albumów");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania albumów." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAlbum(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _albumService.DeleteAlbumAsync(id, userId.Value);
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania albumu");
            return StatusCode(500, new { message = "Wystąpił błąd podczas usuwania albumu." });
        }
    }

    [HttpPost("{albumId}/media")]
    public async Task<ActionResult<AlbumDetailResponseDto>> AddFilesToAlbum(Guid albumId, [FromForm] List<IFormFile> files)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _albumService.AddFilesToAlbumAsync(albumId, userId.Value, files);
            if (result == null)
                return NotFound();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas dodawania plików do albumu");
            return StatusCode(500, new { message = "Wystąpił błąd podczas dodawania plików do albumu." });
        }
    }

    [HttpPost("{albumId}/media/{mediaId}")]
    public async Task<ActionResult> AddMediaToAlbum(Guid albumId, Guid mediaId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _albumService.AddMediaToAlbumAsync(albumId, mediaId, userId.Value);
            if (!result)
                return BadRequest(new { message = "Nie można dodać media do albumu." });

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas dodawania media do albumu");
            return StatusCode(500, new { message = "Wystąpił błąd podczas dodawania media do albumu." });
        }
    }

    [HttpDelete("{albumId}/media/{mediaId}")]
    public async Task<ActionResult> RemoveMediaFromAlbum(Guid albumId, Guid mediaId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _albumService.RemoveMediaFromAlbumAsync(albumId, mediaId, userId.Value);
            if (!result)
                return BadRequest(new { message = "Nie można usunąć media z albumu." });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania media z albumu");
            return StatusCode(500, new { message = "Wystąpił błąd podczas usuwania media z albumu." });
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
