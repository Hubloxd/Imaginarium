from __future__ import annotations

from django.db.models import Q
from django.utils import timezone

from groups.models import GroupMember
from sharing.models import PermissionLevel, Share


def _active_share_q(now=None) -> Q:
    now = now or timezone.now()
    return Q(expires_at__isnull=True) | Q(expires_at__gt=now)


def user_group_ids(user) -> list:
    return list(GroupMember.objects.filter(user=user).values_list("group_id", flat=True))


def shares_accessible_to_user(user):
    """
    Udostępnienia nie-wygasłe, które użytkownik może zobaczyć:
    - publiczne
    - dla użytkownika
    - dla grupy użytkownika
    """
    now = timezone.now()
    group_ids = user_group_ids(user)

    q = Share.objects.filter(_active_share_q(now)).filter(
        Q(is_public=True)
        | Q(shared_with_user=user)
        | Q(shared_with_group_id__in=group_ids)
    )
    return q


def accessible_album_ids(user) -> list:
    return list(
        shares_accessible_to_user(user)
        .filter(album_id__isnull=False)
        .values_list("album_id", flat=True)
        .distinct()
    )


def accessible_media_ids(user) -> list:
    """
    Media dostępne dla użytkownika:
    - bezpośrednio udostępnione media
    - media w udostępnionych albumach
    """
    shares = shares_accessible_to_user(user)
    media_ids = set(
        shares.filter(media_id__isnull=False).values_list("media_id", flat=True).distinct()
    )

    album_ids = shares.filter(album_id__isnull=False).values_list("album_id", flat=True).distinct()
    if album_ids:
        from media_app.models import Media

        album_media_ids = Media.objects.filter(albums__id__in=list(album_ids)).values_list("id", flat=True).distinct()
        media_ids.update(album_media_ids)

    return list(media_ids)


def has_album_edit_permission(user, album_id) -> bool:
    """
    Czy user ma uprawnienie EDIT do albumu przez share (direct/group/public).
    """
    now = timezone.now()
    group_ids = user_group_ids(user)
    return Share.objects.filter(album_id=album_id).filter(_active_share_q(now)).filter(
        Q(permission_level=PermissionLevel.EDIT)
        & (
            Q(is_public=True)
            | Q(shared_with_user=user)
            | Q(shared_with_group_id__in=group_ids)
        )
    ).exists()

