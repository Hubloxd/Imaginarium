from __future__ import annotations

from datetime import datetime

from django.db.models import Q
from django.utils.dateparse import parse_datetime, parse_date
from rest_framework import status
from rest_framework.parsers import FormParser, MultiPartParser
from rest_framework.permissions import IsAuthenticated
from rest_framework.response import Response
from rest_framework.views import APIView

from .models import Media
from .serializers import MediaResponseSerializer
from .services import detect_mime_type, generate_image_thumbnail, guess_media_type
from .tasks import ai_classify_media_task, generate_thumbnail_task
from .utils import safe_delay
from sharing.access import accessible_media_ids


class MediaListView(APIView):
    permission_classes = [IsAuthenticated]
    parser_classes = [MultiPartParser, FormParser]

    def get(self, request):
        shared_media_ids = accessible_media_ids(request.user)
        media = (
            Media.objects.filter(Q(user=request.user) | Q(id__in=shared_media_ids))
            .order_by("-uploaded_at")
            .distinct()
        )
        return Response(MediaResponseSerializer(media, many=True, context={"request": request}).data)

    def post(self, request):
        file = request.FILES.get("file")
        if not file:
            # dla kompatybilności z innymi formami uploadu
            files = request.FILES.getlist("files")
            file = files[0] if files else None

        if not file:
            return Response({"message": "Plik jest wymagany."}, status=status.HTTP_400_BAD_REQUEST)

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

        return Response(MediaResponseSerializer(media, context={"request": request}).data, status=status.HTTP_200_OK)


class MediaSearchView(APIView):
    permission_classes = [IsAuthenticated]

    def get(self, request):
        query = request.query_params.get("query") or ""
        tag = request.query_params.get("tag") or ""
        date_str = request.query_params.get("date")
        album_id = request.query_params.get("albumId")

        shared_media_ids = accessible_media_ids(request.user)
        qs = Media.objects.filter(Q(user=request.user) | Q(id__in=shared_media_ids)).distinct()

        if query.strip():
            qs = qs.filter(Q(file_name__icontains=query.strip()) | Q(mime_type__icontains=query.strip()))

        if tag.strip():
            # Przeszukujemy klasyfikacje AI w JSONField
            # Używamy PostgreSQL JSONB funkcji do wyszukiwania case-insensitive
            tag_lower = tag.strip().lower()
            qs = qs.extra(
                where=[
                    """
                    EXISTS (
                        SELECT 1 FROM jsonb_array_elements(ai_classifications) AS elem
                        WHERE LOWER(elem->>'name') LIKE %s
                    )
                    """
                ],
                params=[f"%{tag_lower}%"]
            ).distinct()

        if date_str:
            # Angular wysyła zwykle YYYY-MM-DD (input[type=date])
            dt = parse_datetime(date_str) or (
                datetime.combine(parse_date(date_str), datetime.min.time()) if parse_date(date_str) else None
            )
            if dt:
                qs = qs.filter(uploaded_at__date=dt.date())

        if album_id:
            qs = qs.filter(album_medias__album_id=album_id).distinct()

        qs = qs.order_by("-uploaded_at")
        return Response(MediaResponseSerializer(qs, many=True, context={"request": request}).data)


class MediaDetailView(APIView):
    permission_classes = [IsAuthenticated]
    parser_classes = [MultiPartParser, FormParser]

    def get_object(self, request, media_id):
        shared_media_ids = accessible_media_ids(request.user)
        return Media.objects.filter(id=media_id).filter(Q(user=request.user) | Q(id__in=shared_media_ids)).first()

    def get(self, request, media_id):
        media = self.get_object(request, media_id)
        if not media:
            return Response(status=status.HTTP_404_NOT_FOUND)
        return Response(MediaResponseSerializer(media, context={"request": request}).data)

    def put(self, request, media_id):
        media = Media.objects.filter(id=media_id, user=request.user).first()
        if not media:
            return Response(status=status.HTTP_404_NOT_FOUND)

        file = request.FILES.get("file")
        if not file:
            return Response({"message": "Plik jest wymagany."}, status=status.HTTP_400_BAD_REQUEST)

        # Usuń stary plik
        if media.file:
            media.file.delete(save=False)

        # Wyczyść stare klasyfikacje AI i miniaturkę (zostaną wygenerowane na nowo)
        media.ai_classifications = []
        media.thumbnail_path = None
        media.width = None
        media.height = None

        media.file_name = file.name
        media.mime_type = file.content_type or detect_mime_type(file.name)
        media.media_type = guess_media_type(media.mime_type)
        media.file_size = file.size
        media.file = file
        media.save()

        # Generuj miniaturkę dla obrazów i filmów
        from .models import MediaType
        if media.media_type == MediaType.IMAGE:
            thumb_path = generate_image_thumbnail(media)
            if thumb_path:
                media.thumbnail_path = thumb_path
                media.save(update_fields=["thumbnail_path", "width", "height", "updated_at"])
            # Wywołaj AI classification asynchronicznie (lub synchronicznie jeśli Celery niedostępny)
            safe_delay(ai_classify_media_task, str(media.id))
        elif media.media_type == MediaType.VIDEO:
            # Dla video generuj miniaturkę asynchronicznie
            safe_delay(generate_thumbnail_task, str(media.id))

        return Response(MediaResponseSerializer(media, context={"request": request}).data)

    def delete(self, request, media_id):
        media = Media.objects.filter(id=media_id, user=request.user).first()
        if not media:
            return Response(status=status.HTTP_404_NOT_FOUND)

        # Usuń pliki
        if media.file:
            media.file.delete(save=False)

        # Usuń miniaturkę jeśli istnieje
        # (thumbnail_path jest absolutną ścieżką)
        try:
            if media.thumbnail_path:
                import os

                if os.path.exists(media.thumbnail_path):
                    os.remove(media.thumbnail_path)
        except Exception:
            pass

        media.delete()
        return Response(status=status.HTTP_204_NO_CONTENT)

