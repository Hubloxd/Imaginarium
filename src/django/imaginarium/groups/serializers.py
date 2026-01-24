from __future__ import annotations

from rest_framework import serializers

from .models import Group, GroupMember


class GroupResponseSerializer(serializers.ModelSerializer):
    id = serializers.UUIDField(read_only=True)
    name = serializers.CharField()
    description = serializers.CharField(allow_null=True, required=False)

    createdByUserId = serializers.SerializerMethodField()
    createdByUsername = serializers.CharField(source="created_by.username")
    createdAt = serializers.DateTimeField(source="created_at")
    updatedAt = serializers.DateTimeField(source="updated_at")
    isPrivate = serializers.BooleanField(source="is_private")
    memberCount = serializers.SerializerMethodField()
    userRole = serializers.SerializerMethodField()

    class Meta:
        model = Group
        fields = [
            "id",
            "name",
            "description",
            "createdByUserId",
            "createdByUsername",
            "createdAt",
            "updatedAt",
            "isPrivate",
            "memberCount",
            "userRole",
        ]

    def get_createdByUserId(self, obj: Group) -> str:
        return str(obj.created_by_id)

    def get_memberCount(self, obj: Group) -> int:
        return obj.group_members.count()

    def get_userRole(self, obj: Group):
        request = self.context.get("request")
        if not request or not getattr(request, "user", None) or request.user.is_anonymous:
            return None
        member = obj.group_members.filter(user=request.user).first()
        return member.role if member else None


class GroupMemberSerializer(serializers.ModelSerializer):
    id = serializers.UUIDField(read_only=True)
    groupId = serializers.UUIDField(source="group_id")
    groupName = serializers.CharField(source="group.name")
    userId = serializers.SerializerMethodField()
    username = serializers.CharField(source="user.username")
    email = serializers.EmailField(source="user.email")
    role = serializers.IntegerField()
    joinedAt = serializers.DateTimeField(source="joined_at")
    invitedByUserId = serializers.SerializerMethodField()
    invitedByUsername = serializers.SerializerMethodField()

    class Meta:
        model = GroupMember
        fields = [
            "id",
            "groupId",
            "groupName",
            "userId",
            "username",
            "email",
            "role",
            "joinedAt",
            "invitedByUserId",
            "invitedByUsername",
        ]

    def get_userId(self, obj: GroupMember) -> str:
        return str(obj.user_id)

    def get_invitedByUserId(self, obj: GroupMember) -> str | None:
        return str(obj.invited_by_id) if obj.invited_by_id else None

    def get_invitedByUsername(self, obj: GroupMember) -> str | None:
        return obj.invited_by.username if obj.invited_by else None

