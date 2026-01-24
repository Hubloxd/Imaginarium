import uuid

from django.conf import settings
from django.db import models


class PermissionLevel(models.IntegerChoices):
    VIEW = 1, "View"
    DOWNLOAD = 2, "Download"
    EDIT = 3, "Edit"


class Share(models.Model):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)

    media = models.ForeignKey("media_app.Media", null=True, blank=True, on_delete=models.CASCADE)
    album = models.ForeignKey("albums.Album", null=True, blank=True, on_delete=models.CASCADE)

    shared_by_user = models.ForeignKey(
        settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name="shares_created"
    )
    shared_with_user = models.ForeignKey(
        settings.AUTH_USER_MODEL, null=True, blank=True, on_delete=models.CASCADE, related_name="shares_received"
    )
    shared_with_group = models.ForeignKey("groups.Group", null=True, blank=True, on_delete=models.CASCADE)

    share_token = models.CharField(max_length=64, unique=True)
    is_public = models.BooleanField(default=False)
    expires_at = models.DateTimeField(null=True, blank=True)
    created_at = models.DateTimeField(auto_now_add=True)
    permission_level = models.IntegerField(choices=PermissionLevel.choices, default=PermissionLevel.VIEW)

    class Meta:
        db_table = "shares"
        indexes = [
            models.Index(fields=["share_token"]),
            models.Index(fields=["shared_by_user"]),
            models.Index(fields=["shared_with_user"]),
            models.Index(fields=["shared_with_group"]),
        ]

