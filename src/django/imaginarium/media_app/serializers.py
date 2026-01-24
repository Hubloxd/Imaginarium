from __future__ import annotations

from django.utils import timezone
from rest_framework import serializers

from .models import Media


class TagDtoSerializer(serializers.Serializer):
    id = serializers.UUIDField()
    name = serializers.CharField()
    category = serializers.CharField(allow_null=True, required=False)
    confidence = serializers.FloatField()
    source = serializers.CharField(allow_null=True, required=False)


class MediaResponseSerializer(serializers.ModelSerializer):
    id = serializers.UUIDField(read_only=True)

    fileName = serializers.CharField(source="file_name")
    filePath = serializers.SerializerMethodField()
    mediaUrl = serializers.SerializerMethodField()
    fileSize = serializers.IntegerField(source="file_size")
    mediaType = serializers.SerializerMethodField()
    mimeType = serializers.CharField(source="mime_type")
    uploadedAt = serializers.DateTimeField(source="uploaded_at")

    width = serializers.IntegerField(required=False, allow_null=True)
    height = serializers.IntegerField(required=False, allow_null=True)
    duration = serializers.IntegerField(required=False, allow_null=True)

    thumbnailPath = serializers.CharField(source="thumbnail_path", allow_null=True, required=False)
    thumbnailUrl = serializers.SerializerMethodField()
    tags = serializers.SerializerMethodField()

    class Meta:
        model = Media
        fields = [
            "id",
            "fileName",
            "filePath",
            "mediaUrl",
            "fileSize",
            "mediaType",
            "mimeType",
            "uploadedAt",
            "width",
            "height",
            "duration",
            "thumbnailPath",
            "thumbnailUrl",
            "tags",
        ]

    def get_filePath(self, obj: Media) -> str:
        try:
            return obj.file.path
        except Exception:
            return ""

    def get_mediaUrl(self, obj: Media) -> str:
        try:
            return obj.file.url
        except Exception:
            return ""

    def get_mediaType(self, obj: Media) -> str:
        return obj.get_media_type_display()

    def get_thumbnailUrl(self, obj: Media) -> str | None:
        if not obj.thumbnail_path:
            return None

        file_name = obj.thumbnail_path.replace("\\", "/").split("/")[-1]
        url = f"/thumbnails/{file_name}"

        updated = obj.updated_at or timezone.now()
        cache_bust = int(updated.timestamp() * 1000)
        return f"{url}?v={cache_bust}"

    def get_tags(self, obj: Media) -> list[dict]:
        # Zwracamy klasyfikacje AI z pola JSONField
        if not obj.ai_classifications:
            return []

        # Konwertujemy klasyfikacje AI na format zgodny z frontendem
        # Frontend oczekuje: id, name, category, confidence, source
        return [
            {
                "id": None,  # Nie ma ID dla klasyfikacji AI
                "name": cls.get("name", ""),
                "category": cls.get("category"),
                "confidence": cls.get("confidence", 0.0),
                "source": "AI",  # Wszystkie klasyfikacje są z AI
            }
            for cls in obj.ai_classifications
        ]

