from __future__ import annotations

from rest_framework import serializers

from .models import Share


class ShareResponseSerializer(serializers.ModelSerializer):
    id = serializers.UUIDField(read_only=True)
    mediaId = serializers.UUIDField(source="media_id", allow_null=True, required=False)
    albumId = serializers.UUIDField(source="album_id", allow_null=True, required=False)

    sharedByUserId = serializers.SerializerMethodField()
    sharedByUsername = serializers.CharField(source="shared_by_user.username")

    sharedWithUserId = serializers.SerializerMethodField()
    sharedWithUsername = serializers.SerializerMethodField()

    sharedWithGroupId = serializers.UUIDField(source="shared_with_group_id", allow_null=True, required=False)
    sharedWithGroupName = serializers.SerializerMethodField()

    shareToken = serializers.CharField(source="share_token")
    isPublic = serializers.BooleanField(source="is_public")
    expiresAt = serializers.DateTimeField(source="expires_at", allow_null=True, required=False)
    createdAt = serializers.DateTimeField(source="created_at")
    permissionLevel = serializers.IntegerField(source="permission_level")

    class Meta:
        model = Share
        fields = [
            "id",
            "mediaId",
            "albumId",
            "sharedByUserId",
            "sharedByUsername",
            "sharedWithUserId",
            "sharedWithUsername",
            "sharedWithGroupId",
            "sharedWithGroupName",
            "shareToken",
            "isPublic",
            "expiresAt",
            "createdAt",
            "permissionLevel",
        ]

    def get_sharedByUserId(self, obj: Share) -> str:
        return str(obj.shared_by_user_id)

    def get_sharedWithUserId(self, obj: Share) -> str | None:
        return str(obj.shared_with_user_id) if obj.shared_with_user_id else None

    def get_sharedWithUsername(self, obj: Share) -> str | None:
        return obj.shared_with_user.username if obj.shared_with_user else None

    def get_sharedWithGroupName(self, obj: Share) -> str | None:
        return obj.shared_with_group.name if obj.shared_with_group else None

