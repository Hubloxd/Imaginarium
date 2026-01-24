from __future__ import annotations

import uuid

from django.contrib.auth import get_user_model
from django.utils import timezone
from django.utils.dateparse import parse_datetime
from rest_framework import status
from rest_framework.permissions import AllowAny, IsAuthenticated
from rest_framework.response import Response
from rest_framework.views import APIView

from albums.models import Album
from albums.serializers import AlbumDetailResponseSerializer
from groups.models import GroupMember
from media_app.models import Media
from media_app.serializers import MediaResponseSerializer
from sharing.models import PermissionLevel, Share
from sharing.serializers import ShareResponseSerializer

User = get_user_model()


def _parse_bool(value) -> bool:
    if isinstance(value, bool):
        return value
    if value is None:
        return False
    s = str(value).strip().lower()
    return s in {"1", "true", "yes", "y", "on"}


def _parse_expires_at(value):
    if not value:
        return None
    if hasattr(value, "tzinfo"):
        return value
    dt = parse_datetime(str(value))
    if not dt:
        return None
    if timezone.is_naive(dt):
        return timezone.make_aware(dt, timezone.get_current_timezone())
    return dt


class ShareCreateView(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request):
        media_id = request.data.get("mediaId")
        album_id = request.data.get("albumId")
        shared_with_user_id = request.data.get("sharedWithUserId")
        shared_with_user_email = request.data.get("sharedWithUserEmail")
        shared_with_group_id = request.data.get("sharedWithGroupId")
        is_public = _parse_bool(request.data.get("isPublic"))
        expires_at = _parse_expires_at(request.data.get("expiresAt"))
        permission_level = request.data.get("permissionLevel", PermissionLevel.VIEW)

        if not media_id and not album_id:
            return Response({"message": "Musi być podane MediaId lub AlbumId"}, status=status.HTTP_400_BAD_REQUEST)
        if media_id and album_id:
            return Response({"message": "Nie można udostępnić jednocześnie Media i Album"}, status=status.HTTP_400_BAD_REQUEST)

        if media_id:
            media = Media.objects.filter(id=media_id, user=request.user).first()
            if not media:
                return Response(status=status.HTTP_403_FORBIDDEN)
        else:
            album = Album.objects.filter(id=album_id, user=request.user).first()
            if not album:
                return Response(status=status.HTTP_403_FORBIDDEN)

        # email -> userId
        if not shared_with_user_id and shared_with_user_email:
            user = User.objects.filter(email=shared_with_user_email).first()
            if not user:
                return Response({"message": "Użytkownik o podanym emailu nie istnieje"}, status=status.HTTP_400_BAD_REQUEST)
            shared_with_user_id = user.pk

        # jeśli nie publiczne, to musi być user lub grupa
        if not is_public and not shared_with_user_id and not shared_with_group_id:
            return Response({"message": "Musi być podany odbiorca lub isPublic=true"}, status=status.HTTP_400_BAD_REQUEST)

        # sprawdź duplikat
        exists = Share.objects.filter(
            media_id=media_id or None,
            album_id=album_id or None,
            shared_with_user_id=shared_with_user_id or None,
            shared_with_group_id=shared_with_group_id or None,
        ).exists()
        if exists:
            return Response({"message": "To udostępnienie już istnieje"}, status=status.HTTP_409_CONFLICT)

        token = uuid.uuid4().hex

        share = Share.objects.create(
            media_id=media_id or None,
            album_id=album_id or None,
            shared_by_user=request.user,
            shared_with_user_id=shared_with_user_id or None,
            shared_with_group_id=shared_with_group_id or None,
            share_token=token,
            is_public=is_public,
            expires_at=expires_at,
            permission_level=permission_level,
        )

        share = (
            Share.objects.filter(id=share.id)
            .select_related("shared_by_user", "shared_with_user", "shared_with_group")
            .first()
        )
        return Response(ShareResponseSerializer(share, context={"request": request}).data)


class ShareByTokenView(APIView):
    permission_classes = [AllowAny]

    def get(self, request, token: str):
        share = (
            Share.objects.filter(share_token=token)
            .select_related("shared_by_user", "shared_with_user", "shared_with_group")
            .first()
        )
        if not share:
            return Response(status=status.HTTP_404_NOT_FOUND)

        if share.expires_at and share.expires_at < timezone.now():
            return Response(status=status.HTTP_404_NOT_FOUND)

        return Response(ShareResponseSerializer(share, context={"request": request}).data)


class ShareValidateView(APIView):
    permission_classes = [AllowAny]

    def get(self, request, token: str):
        share = Share.objects.filter(share_token=token).first()
        if not share:
            return Response(False)

        if share.expires_at and share.expires_at < timezone.now():
            return Response(False)

        if share.is_public:
            return Response(True)

        user = request.user if request.user.is_authenticated else None
        if not user:
            return Response(False)

        if share.shared_with_user_id and str(share.shared_with_user_id) == str(user.pk):
            return Response(True)

        if share.shared_with_group_id:
            is_member = GroupMember.objects.filter(group_id=share.shared_with_group_id, user=user).exists()
            return Response(bool(is_member))

        return Response(False)


class ShareRevokeView(APIView):
    permission_classes = [IsAuthenticated]

    def delete(self, request, share_id):
        share = Share.objects.filter(id=share_id).first()
        if not share:
            return Response(status=status.HTTP_404_NOT_FOUND)

        if share.shared_by_user_id != request.user.id:
            return Response(status=status.HTTP_404_NOT_FOUND)

        share.delete()
        return Response(status=status.HTTP_204_NO_CONTENT)


class ShareWithGroupView(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request):
        group_id = request.data.get("sharedWithGroupId")
        if not group_id:
            return Response({"message": "SharedWithGroupId jest wymagane dla ShareWithGroup"}, status=status.HTTP_400_BAD_REQUEST)

        is_member = GroupMember.objects.filter(group_id=group_id, user=request.user).exists()
        if not is_member:
            return Response(status=status.HTTP_403_FORBIDDEN)

        return ShareCreateView().post(request)


class GroupSharesView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request, group_id):
        # Bezpieczniej: pokaż tylko członkom grupy
        if not GroupMember.objects.filter(group_id=group_id, user=request.user).exists():
            return Response([])

        shares = (
            Share.objects.filter(shared_with_group_id=group_id)
            .select_related("shared_by_user", "shared_with_user", "shared_with_group")
            .order_by("-created_at")
        )
        return Response(ShareResponseSerializer(shares, many=True, context={"request": request}).data)


class SharesForMeView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        shares = Share.objects.filter(shared_with_user=request.user)

        group_ids = (
            GroupMember.objects.filter(user=request.user)
            .values_list("group_id", flat=True)
        )
        if group_ids:
            shares = shares.union(Share.objects.filter(shared_with_group_id__in=list(group_ids)))

        shares = (
            Share.objects.filter(id__in=shares.values_list("id", flat=True))
            .select_related("shared_by_user", "shared_with_user", "shared_with_group")
            .order_by("-created_at")
        )
        return Response(ShareResponseSerializer(shares, many=True, context={"request": request}).data)


class SharesByMeView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        shares = (
            Share.objects.filter(shared_by_user=request.user)
            .select_related("shared_by_user", "shared_with_user", "shared_with_group")
            .order_by("-created_at")
        )
        return Response(ShareResponseSerializer(shares, many=True, context={"request": request}).data)


class ShareContentByTokenView(APIView):
    """
    Publiczny endpoint do pobierania treści (media/album) przez token.
    Dostępny bez autentykacji dla publicznych udostępnień.
    """
    permission_classes = [AllowAny]

    def get(self, request, token: str):
        share = Share.objects.filter(share_token=token).first()
        if not share:
            return Response(status=status.HTTP_404_NOT_FOUND)

        # Sprawdź czy share nie wygasł
        if share.expires_at and share.expires_at < timezone.now():
            return Response(status=status.HTTP_404_NOT_FOUND)

        # Sprawdź uprawnienia dostępu
        if share.is_public:
            # Publiczne - dostępne dla wszystkich
            pass
        else:
            # Prywatne - wymaga autentykacji i sprawdzenia uprawnień
            if not request.user.is_authenticated:
                return Response(status=status.HTTP_401_UNAUTHORIZED)
            
            user = request.user
            has_access = False
            
            if share.shared_with_user_id and str(share.shared_with_user_id) == str(user.pk):
                has_access = True
            elif share.shared_with_group_id:
                has_access = GroupMember.objects.filter(group_id=share.shared_with_group_id, user=user).exists()
            
            if not has_access:
                return Response(status=status.HTTP_403_FORBIDDEN)

        # Zwróć treść w zależności od typu
        if share.media_id:
            media = Media.objects.filter(id=share.media_id).first()
            if not media:
                return Response(status=status.HTTP_404_NOT_FOUND)
            return Response(MediaResponseSerializer(media, context={"request": request}).data)
        elif share.album_id:
            album = (
                Album.objects.filter(id=share.album_id)
                .prefetch_related("media")
                .select_related("cover_media")
                .first()
            )
            if not album:
                return Response(status=status.HTTP_404_NOT_FOUND)
            return Response(AlbumDetailResponseSerializer(album, context={"request": request}).data)
        
        return Response(status=status.HTTP_404_NOT_FOUND)

