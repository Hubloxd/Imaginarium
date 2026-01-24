from __future__ import annotations

import base64

import requests
from celery import shared_task
from django.conf import settings

from media_app.models import Media, MediaType
from media_app.services import generate_image_thumbnail, generate_video_thumbnail


@shared_task(bind=True, autoretry_for=(Exception,), retry_backoff=True, max_retries=5)
def generate_thumbnail_task(self, media_id: str) -> None:
    media = Media.objects.filter(id=media_id).first()
    if not media:
        return
    if not media.file:
        return

    thumb_path = None
    if media.media_type == MediaType.IMAGE:
        thumb_path = generate_image_thumbnail(media)
    elif media.media_type == MediaType.VIDEO:
        thumb_path = generate_video_thumbnail(media)

    if not thumb_path:
        return

    media.thumbnail_path = thumb_path
    media.save(update_fields=["thumbnail_path", "width", "height", "updated_at"])


@shared_task(bind=True, autoretry_for=(Exception,), retry_backoff=True, max_retries=5)
def ai_classify_media_task(self, media_id: str) -> None:
    media = Media.objects.filter(id=media_id).first()
    if not media:
        return
    if media.media_type != 1:  # IMAGE
        return
    if not media.file:
        return

    with open(media.file.path, "rb") as f:
        image_b64 = base64.b64encode(f.read()).decode("utf-8")

    payload = {"image_base64": image_b64, "media_id": str(media.id)}
    resp = requests.post(settings.AI_SERVICE_URL, json=payload, timeout=60)
    resp.raise_for_status()
    data = resp.json()

    predictions = data.get("predictions") or []

    # Zapisujemy klasyfikacje AI bezpośrednio do pola JSONField
    classifications = []
    for pred in predictions:
        name = pred.get("class") or pred.get("name") or pred.get("category")
        if not name:
            continue
        confidence = float(pred.get("confidence") or 0.0)
        category = pred.get("category") or None

        classifications.append({
            "name": str(name),
            "confidence": confidence,
            "category": category,
        })

    # Sortujemy po confidence (malejąco)
    classifications.sort(key=lambda x: x["confidence"], reverse=True)

    media.ai_classifications = classifications
    media.save(update_fields=["ai_classifications", "updated_at"])

