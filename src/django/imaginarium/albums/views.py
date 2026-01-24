from __future__ import annotations

from rest_framework import status
from rest_framework.parsers import FormParser, MultiPartParser
from rest_framework.permissions import IsAuthenticated
from rest_framework.response import Response
from rest_framework.views import APIView
from django.db import models

from albums.models import Album
from albums.serializers import AlbumDetailResponseSerializer, AlbumResponseSerializer
from media_app.models import Media
from media_app.services import detect_mime_type, guess_media_type
from media_app.tasks import ai_classify_media_task, generate_thumbnail_task
from media_app.utils import safe_delay
from sharing.access import accessible_album_ids, has_album_edit_permission


class AlbumListCreateView(APIView):
    permission_classes = [IsAuthenticated]
    parser_classes = [MultiPartParser, FormParser]

    def get(self, request):
        shared_ids = accessible_album_ids(request.user)
        albums = (
            Album.objects.filter(models.Q(user=request.user) | models.Q(id__in=shared_ids))
            .select_related("cover_media")
            .order_by("-created_at")
            .distinct()
        )
        return Response(AlbumResponseSerializer(albums, many=True, context={"request": request}).data)

    def post(self, request):
        name = (request.data.get("name") or "").strip()
        description = request.data.get("description")
        files = request.FILES.getlist("files")

        if not name:
            return Response({"message": "Nazwa albumu jest wymagana."}, status=status.HTTP_400_BAD_REQUEST)

        album = Album.objects.create(user=request.user, name=name, description=description or None)

        created_media = []
        for file in files:
            mime = file.content_type or detect_mime_type(file.name)
            media = Media.objects.create(
                user=request.user,
                file_name=file.name,
                file=file,
                file_size=file.size,
                mime_type=mime,
                media_type=guess_media_type(mime),
            )

            # Generuj miniaturkę dla obrazów i filmów
            safe_delay(generate_thumbnail_task, str(media.id))
            
            # Klasyfikacja AI tylko dla obrazów
            if media.media_type == 1:  # IMAGE
                safe_delay(ai_classify_media_task, str(media.id))

            album.media.add(media)
            created_media.append(media)

        if not album.cover_media and created_media:
            album.cover_media = created_media[0]
            album.save(update_fields=["cover_media", "updated_at"])

        return Response(AlbumResponseSerializer(album, context={"request": request}).data, status=status.HTTP_200_OK)


class AlbumDetailView(APIView):
    permission_classes = [IsAuthenticated]

    def get_object(self, request, album_id):
        shared_ids = accessible_album_ids(request.user)
        return (
            Album.objects.filter(id=album_id)
            .filter(models.Q(user=request.user) | models.Q(id__in=shared_ids))
            .prefetch_related("media")
            .select_related("cover_media")
            .first()
        )

    def get(self, request, album_id):
        album = self.get_object(request, album_id)
        if not album:
            return Response(status=status.HTTP_404_NOT_FOUND)
        return Response(AlbumDetailResponseSerializer(album, context={"request": request}).data)

    def delete(self, request, album_id):
        album = Album.objects.filter(id=album_id).first()
        if not album:
            return Response(status=status.HTTP_404_NOT_FOUND)
        
        # Sprawdź uprawnienia: użytkownik musi być właścicielem lub mieć uprawnienia edycji
        if album.user_id != request.user.id and not has_album_edit_permission(request.user, album.id):
            return Response(status=status.HTTP_404_NOT_FOUND)
        
        album.delete()
        return Response(status=status.HTTP_204_NO_CONTENT)


class AlbumAddFilesView(APIView):
    permission_classes = [IsAuthenticated]
    parser_classes = [MultiPartParser, FormParser]

    def post(self, request, album_id):
        album = Album.objects.filter(id=album_id).first()
        if not album:
            return Response(status=status.HTTP_404_NOT_FOUND)

        if album.user_id != request.user.id and not has_album_edit_permission(request.user, album.id):
            return Response(status=status.HTTP_404_NOT_FOUND)

        files = request.FILES.getlist("files")

        for file in files:
            mime = file.content_type or detect_mime_type(file.name)
            media = Media.objects.create(
                user=request.user,
                file_name=file.name,
                file=file,
                file_size=file.size,
                mime_type=mime,
                media_type=guess_media_type(mime),
            )
            # Generuj miniaturkę dla obrazów i filmów
            safe_delay(generate_thumbnail_task, str(media.id))
            
            # Klasyfikacja AI tylko dla obrazów
            if media.media_type == 1:  # IMAGE
                safe_delay(ai_classify_media_task, str(media.id))

            album.media.add(media)

        if not album.cover_media:
            first_media = album.media.first()
            if first_media:
                album.cover_media = first_media
                album.save(update_fields=["cover_media", "updated_at"])

        album = (
            Album.objects.filter(id=album_id, user=request.user)
            .prefetch_related("media")
            .select_related("cover_media")
            .first()
        )
        return Response(AlbumDetailResponseSerializer(album, context={"request": request}).data)


class AlbumMediaLinkView(APIView):
    permission_classes = [IsAuthenticated]

    def post(self, request, album_id, media_id):
        album = Album.objects.filter(id=album_id).first()
        if not album:
            return Response(status=status.HTTP_404_NOT_FOUND)

        if album.user_id != request.user.id and not has_album_edit_permission(request.user, album.id):
            return Response(status=status.HTTP_404_NOT_FOUND)

        media = Media.objects.filter(id=media_id, user=request.user).first()
        if not media:
            return Response({"message": "Nie można dodać media do albumu."}, status=status.HTTP_400_BAD_REQUEST)

        if album.media.filter(id=media_id).exists():
            return Response({"message": "Nie można dodać media do albumu."}, status=status.HTTP_400_BAD_REQUEST)

        album.media.add(media)

        if not album.cover_media:
            album.cover_media = media
            album.save(update_fields=["cover_media", "updated_at"])

        return Response(status=status.HTTP_200_OK)


    def delete(self, request, album_id, media_id):
        album = Album.objects.filter(id=album_id).select_related("cover_media").first()
        if not album:
            return Response({"message": "Nie można usunąć media z albumu."}, status=status.HTTP_400_BAD_REQUEST)

        if album.user_id != request.user.id and not has_album_edit_permission(request.user, album.id):
            return Response({"message": "Nie można usunąć media z albumu."}, status=status.HTTP_400_BAD_REQUEST)

        media = album.media.filter(id=media_id).first()
        if not media:
            return Response({"message": "Nie można usunąć media z albumu."}, status=status.HTTP_400_BAD_REQUEST)

        album.media.remove(media)

        if album.cover_media_id == media_id:
            first_media = album.media.first()
            album.cover_media = first_media
            album.save(update_fields=["cover_media", "updated_at"])

        return Response(status=status.HTTP_204_NO_CONTENT)

