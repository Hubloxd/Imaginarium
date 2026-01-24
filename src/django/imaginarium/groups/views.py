from __future__ import annotations

from django.contrib.auth import get_user_model
from rest_framework import status
from rest_framework.permissions import IsAuthenticated
from rest_framework.response import Response
from rest_framework.views import APIView

from .models import Group, GroupMember, GroupRole
from .serializers import GroupMemberSerializer, GroupResponseSerializer

User = get_user_model()


def _get_member(group: Group, user) -> GroupMember | None:
    return group.group_members.filter(user=user).select_related("user").first()

def _parse_bool(value) -> bool:
    if isinstance(value, bool):
        return value
    if value is None:
        return False
    s = str(value).strip().lower()
    return s in {"1", "true", "yes", "y", "on"}


class GroupListCreateView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        groups = (
            Group.objects.filter(group_members__user=request.user)
            .select_related("created_by")
            .prefetch_related("group_members")
            .distinct()
            .order_by("-created_at")
        )
        return Response(GroupResponseSerializer(groups, many=True, context={"request": request}).data)

    def post(self, request):
        name = (request.data.get("name") or "").strip()
        description = request.data.get("description")
        is_private = _parse_bool(request.data.get("isPrivate"))

        if not name:
            return Response({"message": "Nazwa grupy jest wymagana."}, status=status.HTTP_400_BAD_REQUEST)

        group = Group.objects.create(
            name=name,
            description=description or None,
            created_by=request.user,
            is_private=is_private,
        )

        GroupMember.objects.create(group=group, user=request.user, role=GroupRole.ADMIN, invited_by=None)
        group = Group.objects.filter(id=group.id).select_related("created_by").prefetch_related("group_members").first()
        return Response(GroupResponseSerializer(group, context={"request": request}).data)


class GroupDetailView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request, group_id):
        group = (
            Group.objects.filter(id=group_id)
            .select_related("created_by")
            .prefetch_related("group_members")
            .first()
        )
        if not group:
            return Response(status=status.HTTP_404_NOT_FOUND)

        is_member = group.group_members.filter(user=request.user).exists()
        if group.is_private and not is_member:
            return Response(status=status.HTTP_404_NOT_FOUND)

        return Response(GroupResponseSerializer(group, context={"request": request}).data)

    def delete(self, request, group_id):
        group = Group.objects.filter(id=group_id).first()
        if not group:
            return Response(status=status.HTTP_404_NOT_FOUND)

        if group.created_by_id != request.user.id:
            return Response(status=status.HTTP_404_NOT_FOUND)

        group.delete()
        return Response(status=status.HTTP_204_NO_CONTENT)


class GroupInvitationsView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        invitations = (
            GroupMember.objects.filter(user=request.user, invited_by__isnull=False)
            .select_related("group", "user", "invited_by", "group__created_by")
            .order_by("-joined_at")
        )
        return Response(GroupMemberSerializer(invitations, many=True, context={"request": request}).data)


class GroupInviteUserView(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request, group_id):
        group = Group.objects.filter(id=group_id).prefetch_related("group_members").first()
        if not group:
            return Response({"message": "Nie można zaprosić użytkownika do grupy."}, status=status.HTTP_400_BAD_REQUEST)

        inviter = _get_member(group, request.user)
        if not inviter or inviter.role == GroupRole.VIEWER:
            return Response({"message": "Nie można zaprosić użytkownika do grupy."}, status=status.HTTP_400_BAD_REQUEST)

        email = (request.data.get("email") or "").strip()
        role = request.data.get("role", GroupRole.MEMBER)

        if not email:
            return Response({"message": "Email jest wymagany."}, status=status.HTTP_400_BAD_REQUEST)

        invited_user = User.objects.filter(email=email).first()
        if not invited_user:
            return Response({"message": "Nie można zaprosić użytkownika do grupy."}, status=status.HTTP_400_BAD_REQUEST)

        if GroupMember.objects.filter(group=group, user=invited_user).exists():
            return Response({"message": "Nie można zaprosić użytkownika do grupy."}, status=status.HTTP_400_BAD_REQUEST)

        GroupMember.objects.create(group=group, user=invited_user, role=role, invited_by=request.user)
        return Response(status=status.HTTP_200_OK)


class GroupAcceptInvitationView(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request, group_id):
        member = GroupMember.objects.filter(group_id=group_id, user=request.user).select_related("invited_by").first()
        if not member or member.invited_by_id is None:
            return Response({"message": "Nie można zaakceptować zaproszenia."}, status=status.HTTP_400_BAD_REQUEST)

        member.invited_by = None
        member.save(update_fields=["invited_by"])
        return Response(status=status.HTTP_200_OK)


class GroupRejectInvitationView(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request, group_id):
        member = GroupMember.objects.filter(group_id=group_id, user=request.user).first()
        if not member or member.invited_by_id is None:
            return Response({"message": "Nie można odrzucić zaproszenia."}, status=status.HTTP_400_BAD_REQUEST)

        member.delete()
        return Response(status=status.HTTP_200_OK)


class GroupMembersView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request, group_id):
        group = Group.objects.filter(id=group_id).first()
        if not group:
            return Response([])

        is_member = GroupMember.objects.filter(group=group, user=request.user).exists()
        if group.is_private and not is_member:
            return Response([])

        members = (
            GroupMember.objects.filter(group=group)
            .select_related("group", "user", "invited_by")
            .order_by("joined_at")
        )
        return Response(GroupMemberSerializer(members, many=True, context={"request": request}).data)


class GroupRemoveMemberView(APIView):
    permission_classes = [IsAuthenticated]

    def delete(self, request, group_id, member_id):
        group = Group.objects.filter(id=group_id).prefetch_related("group_members").first()
        if not group:
            return Response({"message": "Nie można usunąć członka z grupy."}, status=status.HTTP_400_BAD_REQUEST)

        requester = _get_member(group, request.user)
        if not requester or requester.role != GroupRole.ADMIN:
            return Response({"message": "Nie można usunąć członka z grupy."}, status=status.HTTP_400_BAD_REQUEST)

        # Nie można usunąć samego siebie
        if str(request.user.id) == str(member_id):
            return Response({"message": "Nie można usunąć członka z grupy."}, status=status.HTTP_400_BAD_REQUEST)

        target_user = User.objects.filter(pk=member_id).first()
        if not target_user:
            return Response({"message": "Nie można usunąć członka z grupy."}, status=status.HTTP_400_BAD_REQUEST)

        deleted, _ = GroupMember.objects.filter(group=group, user=target_user).delete()
        if not deleted:
            return Response({"message": "Nie można usunąć członka z grupy."}, status=status.HTTP_400_BAD_REQUEST)

        return Response(status=status.HTTP_204_NO_CONTENT)


class GroupUpdateMemberRoleView(APIView):
    permission_classes = [IsAuthenticated]

    def put(self, request, group_id, member_id):
        group = Group.objects.filter(id=group_id).prefetch_related("group_members").first()
        if not group:
            return Response({"message": "Nie można zaktualizować roli członka."}, status=status.HTTP_400_BAD_REQUEST)

        requester = _get_member(group, request.user)
        if not requester or requester.role != GroupRole.ADMIN:
            return Response({"message": "Nie można zaktualizować roli członka."}, status=status.HTTP_400_BAD_REQUEST)

        role = request.data.get("role")
        if role is None:
            return Response({"message": "Nie można zaktualizować roli członka."}, status=status.HTTP_400_BAD_REQUEST)

        target_user = User.objects.filter(pk=member_id).first()
        if not target_user:
            return Response({"message": "Nie można zaktualizować roli członka."}, status=status.HTTP_400_BAD_REQUEST)

        updated = GroupMember.objects.filter(group=group, user=target_user).update(role=role)
        if not updated:
            return Response({"message": "Nie można zaktualizować roli członka."}, status=status.HTTP_400_BAD_REQUEST)

        return Response(status=status.HTTP_200_OK)


class GroupLeaveView(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request, group_id):
        group = Group.objects.filter(id=group_id).first()
        if not group:
            return Response({"message": "Nie można opuścić grupy."}, status=status.HTTP_400_BAD_REQUEST)

        if group.created_by_id == request.user.id:
            return Response({"message": "Nie można opuścić grupy."}, status=status.HTTP_400_BAD_REQUEST)

        deleted, _ = GroupMember.objects.filter(group=group, user=request.user).delete()
        if not deleted:
            return Response({"message": "Nie można opuścić grupy."}, status=status.HTTP_400_BAD_REQUEST)

        return Response(status=status.HTTP_200_OK)

