from __future__ import annotations

from rest_framework import serializers

from albums.models import Album
from media_app.serializers import MediaResponseSerializer


class AlbumResponseSerializer(serializers.ModelSerializer):
    id = serializers.UUIDField(read_only=True)
    name = serializers.CharField()
    description = serializers.CharField(required=False, allow_null=True)

    createdAt = serializers.DateTimeField(source="created_at")
    updatedAt = serializers.DateTimeField(source="updated_at")
    coverMediaId = serializers.UUIDField(source="cover_media_id", allow_null=True, required=False)
    coverThumbnailUrl = serializers.SerializerMethodField()
    mediaCount = serializers.SerializerMethodField()
    userId = serializers.UUIDField(source="user_id", read_only=True)

    class Meta:
        model = Album
        fields = [
            "id",
            "name",
            "description",
            "createdAt",
            "updatedAt",
            "coverMediaId",
            "coverThumbnailUrl",
            "mediaCount",
            "userId",
        ]

    def get_coverThumbnailUrl(self, obj: Album) -> str | None:
        if not obj.cover_media or not obj.cover_media.thumbnail_path:
            return None
        # Używamy takiej samej logiki URL jak w MediaResponseSerializer
        file_name = obj.cover_media.thumbnail_path.replace("\\", "/").split("/")[-1]
        updated = obj.cover_media.updated_at
        cache_bust = int(updated.timestamp() * 1000) if updated else 0
        return f"/thumbnails/{file_name}?v={cache_bust}"

    def get_mediaCount(self, obj: Album) -> int:
        return getattr(obj, "media_count", None) or obj.media.count()


class AlbumDetailResponseSerializer(AlbumResponseSerializer):
    media = serializers.SerializerMethodField()

    class Meta(AlbumResponseSerializer.Meta):
        fields = AlbumResponseSerializer.Meta.fields + ["media"]

    def get_media(self, obj: Album):
        # Pobierz wszystkie media z albumu
        medias = obj.media.all()
        return MediaResponseSerializer(medias, many=True, context=self.context).data

