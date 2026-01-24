import mimetypes
import subprocess
from pathlib import Path

from django.conf import settings
from PIL import Image, ImageOps

from .models import Media, MediaType


def guess_media_type(mime_type: str) -> MediaType:
    if mime_type and mime_type.startswith("video/"):
        return MediaType.VIDEO
    return MediaType.IMAGE


def ensure_dir(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def generate_image_thumbnail(media: Media, size: tuple[int, int] = (512, 512)) -> str | None:
    """
    Generuje miniaturę JPG do THUMBNAILS_ROOT w formacie: {media.id}.jpg
    Zwraca absolutną ścieżkę do pliku lub None.
    """
    if not media.file:
        return None

    try:
        thumbnails_root = Path(settings.THUMBNAILS_ROOT)
        ensure_dir(thumbnails_root)
        thumb_path = thumbnails_root / f"{media.id}.jpg"

        with Image.open(media.file.path) as img:
            img = ImageOps.exif_transpose(img)
            media.width, media.height = img.size

            img.thumbnail(size)
            img = img.convert("RGB")
            img.save(thumb_path, format="JPEG", quality=85, optimize=True)

        return str(thumb_path)
    except Exception:
        # Jeśli coś pójdzie nie tak (np. nieobsługiwany format), po prostu nie ustawiamy miniatury.
        return None


def generate_video_thumbnail(media: Media, size: tuple[int, int] = (512, 512)) -> str | None:
    """
    Generuje miniaturę JPG dla filmu używając ffmpeg.
    Wyciąga klatkę z 1 sekundy filmu (lub z początku jeśli film jest krótszy).
    Zwraca absolutną ścieżkę do pliku lub None.
    """
    if not media.file:
        return None

    try:
        thumbnails_root = Path(settings.THUMBNAILS_ROOT)
        ensure_dir(thumbnails_root)
        thumb_path = thumbnails_root / f"{media.id}.jpg"

        # Używamy ffmpeg do wyciągnięcia klatki z filmu
        # -ss 1: zaczynamy od 1 sekundy (pomijamy potencjalne czarne klatki na początku)
        # -vframes 1: wyciągamy tylko jedną klatkę
        # -vf scale: skalujemy do maksymalnego rozmiaru zachowując proporcje
        # -q:v 2: wysoka jakość JPEG
        cmd = [
            "ffmpeg",
            "-i", media.file.path,
            "-ss", "00:00:01",  # 1 sekunda
            "-vframes", "1",
            "-vf", f"scale={size[0]}:{size[1]}:force_original_aspect_ratio=decrease",
            "-q:v", "2",
            "-y",  # Nadpisz jeśli istnieje
            str(thumb_path),
        ]

        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=30,
        )

        if result.returncode != 0:
            # Jeśli film jest krótszy niż 1 sekunda, spróbuj od początku
            cmd[3] = "00:00:00"
            result = subprocess.run(
                cmd,
                capture_output=True,
                text=True,
                timeout=30,
            )

        if result.returncode != 0:
            return None

        # Pobierz wymiary z wygenerowanego obrazu
        with Image.open(thumb_path) as img:
            media.width, media.height = img.size

        return str(thumb_path)
    except Exception:
        # Jeśli coś pójdzie nie tak (np. ffmpeg nie jest dostępny), po prostu nie ustawiamy miniatury.
        return None


def detect_mime_type(file_name: str) -> str:
    mime, _ = mimetypes.guess_type(file_name)
    return mime or "application/octet-stream"

