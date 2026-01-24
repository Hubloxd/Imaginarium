import uuid

from django.conf import settings
from django.db import models


class GroupRole(models.IntegerChoices):
    ADMIN = 1, "Admin"
    MEMBER = 2, "Member"
    VIEWER = 3, "Viewer"


class Group(models.Model):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    name = models.CharField(max_length=255)
    description = models.TextField(null=True, blank=True)

    created_by = models.ForeignKey(
        settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name="created_groups"
    )
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)
    is_private = models.BooleanField(default=True)

    class Meta:
        db_table = "groups"
        indexes = [
            models.Index(fields=["created_by"]),
            models.Index(fields=["is_private"]),
        ]

    def __str__(self) -> str:
        return self.name


class GroupMember(models.Model):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    group = models.ForeignKey("groups.Group", on_delete=models.CASCADE, related_name="group_members")
    user = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name="group_memberships")
    role = models.IntegerField(choices=GroupRole.choices, default=GroupRole.MEMBER)
    joined_at = models.DateTimeField(auto_now_add=True)

    invited_by = models.ForeignKey(
        settings.AUTH_USER_MODEL,
        on_delete=models.SET_NULL,
        null=True,
        blank=True,
        related_name="sent_group_invitations",
    )

    class Meta:
        db_table = "group_members"
        unique_together = [
            ("group", "user"),
        ]
        indexes = [
            models.Index(fields=["group"]),
            models.Index(fields=["user"]),
        ]

