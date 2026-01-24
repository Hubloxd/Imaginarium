import uuid

from django.conf import settings
from django.db import models


class MediaType(models.IntegerChoices):
    IMAGE = 1, "Image"
    VIDEO = 2, "Video"


def upload_to_user_folder(instance: "Media", filename: str) -> str:
    return f"uploads/{instance.user_id}/{uuid.uuid4()}_{filename}"


class Media(models.Model):
    id = models.UUIDField(primary_key=True, default=uuid.uuid4, editable=False)
    user = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name="media")

    file_name = models.CharField(max_length=255)
    file = models.FileField(upload_to=upload_to_user_folder)
    file_size = models.BigIntegerField()
    media_type = models.IntegerField(choices=MediaType.choices)
    mime_type = models.CharField(max_length=100)

    uploaded_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    width = models.IntegerField(null=True, blank=True)
    height = models.IntegerField(null=True, blank=True)
    duration = models.IntegerField(null=True, blank=True)

    thumbnail_path = models.CharField(max_length=500, null=True, blank=True)

    # Klasyfikacje AI - lista słowników z polami: name, confidence, category
    # Przykład: [{"name": "dog", "confidence": 0.95, "category": "animal"}, ...]
    ai_classifications = models.JSONField(default=list, blank=True, null=True)

    class Meta:
        db_table = "media"
        indexes = [
            models.Index(fields=["user", "uploaded_at"]),
            models.Index(fields=["media_type"]),
        ]

    def __str__(self) -> str:
        return self.file_name

