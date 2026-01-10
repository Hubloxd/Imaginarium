using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ImaginariumBackend.DTOs;
using ImaginariumBackend.Services;
using ImaginariumBackend.Models;

namespace ImaginariumBackend.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
[EnableCors("AllowAngular")]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly ILogger<GroupController> _logger;

    public GroupController(IGroupService groupService, ILogger<GroupController> logger)
    {
        _groupService = groupService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<GroupResponseDto>> CreateGroup([FromBody] CreateGroupDto createDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _groupService.CreateGroupAsync(userId.Value, createDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia grupy");
            return StatusCode(500, new { message = "Wystąpił błąd podczas tworzenia grupy." });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GroupResponseDto>> GetGroup(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var group = await _groupService.GetGroupByIdAsync(id, userId.Value);
            if (group == null)
                return NotFound();

            return Ok(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania grupy");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania grupy." });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<GroupResponseDto>>> GetUserGroups()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var groups = await _groupService.GetUserGroupsAsync(userId.Value);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania grup");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania grup." });
        }
    }

    [HttpGet("invitations")]
    public async Task<ActionResult<List<GroupMemberDto>>> GetPendingInvitations()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var invitations = await _groupService.GetPendingInvitationsAsync(userId.Value);
            return Ok(invitations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania zaproszeń");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania zaproszeń." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteGroup(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _groupService.DeleteGroupAsync(id, userId.Value);
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania grupy");
            return StatusCode(500, new { message = "Wystąpił błąd podczas usuwania grupy." });
        }
    }

    [HttpPost("{groupId}/invite")]
    public async Task<ActionResult> InviteUser(Guid groupId, [FromBody] InviteUserDto inviteDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _groupService.InviteUserAsync(groupId, userId.Value, inviteDto);
            if (!result)
                return BadRequest(new { message = "Nie można zaprosić użytkownika do grupy." });

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zapraszania użytkownika");
            return StatusCode(500, new { message = "Wystąpił błąd podczas zapraszania użytkownika." });
        }
    }

    [HttpPost("{groupId}/accept")]
    public async Task<ActionResult> AcceptInvitation(Guid groupId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _groupService.AcceptInvitationAsync(groupId, userId.Value);
            if (!result)
                return BadRequest(new { message = "Nie można zaakceptować zaproszenia." });

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas akceptowania zaproszenia");
            return StatusCode(500, new { message = "Wystąpił błąd podczas akceptowania zaproszenia." });
        }
    }

    [HttpPost("{groupId}/reject")]
    public async Task<ActionResult> RejectInvitation(Guid groupId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _groupService.RejectInvitationAsync(groupId, userId.Value);
            if (!result)
                return BadRequest(new { message = "Nie można odrzucić zaproszenia." });

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas odrzucania zaproszenia");
            return StatusCode(500, new { message = "Wystąpił błąd podczas odrzucania zaproszenia." });
        }
    }

    [HttpGet("{groupId}/members")]
    public async Task<ActionResult<List<GroupMemberDto>>> GetGroupMembers(Guid groupId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var members = await _groupService.GetGroupMembersAsync(groupId, userId.Value);
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania członków grupy");
            return StatusCode(500, new { message = "Wystąpił błąd podczas pobierania członków grupy." });
        }
    }

    [HttpDelete("{groupId}/members/{memberId}")]
    public async Task<ActionResult> RemoveMember(Guid groupId, Guid memberId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _groupService.RemoveMemberAsync(groupId, memberId, userId.Value);
            if (!result)
                return BadRequest(new { message = "Nie można usunąć członka z grupy." });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania członka");
            return StatusCode(500, new { message = "Wystąpił błąd podczas usuwania członka." });
        }
    }

    [HttpPut("{groupId}/members/{memberId}/role")]
    public async Task<ActionResult> UpdateMemberRole(Guid groupId, Guid memberId, [FromBody] UpdateMemberRoleDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _groupService.UpdateMemberRoleAsync(groupId, memberId, dto.Role, userId.Value);
            if (!result)
                return BadRequest(new { message = "Nie można zaktualizować roli członka." });

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji roli członka");
            return StatusCode(500, new { message = "Wystąpił błąd podczas aktualizacji roli członka." });
        }
    }

    [HttpPost("{groupId}/leave")]
    public async Task<ActionResult> LeaveGroup(Guid groupId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _groupService.LeaveGroupAsync(groupId, userId.Value);
            if (!result)
                return BadRequest(new { message = "Nie można opuścić grupy." });

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas opuszczania grupy");
            return StatusCode(500, new { message = "Wystąpił błąd podczas opuszczania grupy." });
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

public class UpdateMemberRoleDto
{
    public GroupRoleEnum Role { get; set; }
}
