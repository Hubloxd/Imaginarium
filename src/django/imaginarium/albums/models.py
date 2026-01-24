import uuid

from django.conf import settings
from django.db import models


class Album(models.Model):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    user = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name="albums")

    name = models.CharField(max_length=255)
    description = models.TextField(max_length=1000, null=True, blank=True)

    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    cover_media = models.ForeignKey(
        "media_app.Media",
        null=True,
        blank=True,
        on_delete=models.SET_NULL,
        related_name="cover_for_albums",
    )

    media = models.ManyToManyField("media_app.Media", related_name="albums", blank=True)

    class Meta:
        db_table = "albums"
        indexes = [
            models.Index(fields=["user", "created_at"]),
        ]

    def __str__(self) -> str:
        return self.name

