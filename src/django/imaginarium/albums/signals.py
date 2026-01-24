from __future__ import annotations

import os

from django.db.models.signals import pre_delete
from django.dispatch import receiver

from .models import Album


@receiver(pre_delete, sender=Album)
def delete_album_media(sender, instance: Album, **kwargs):
    """
    Przy usuwaniu albumu usuwa wszystkie media, które są TYLKO w tym albumie.
    Media, które są w innych albumach, pozostają nietknięte.
    """
    # Pobierz wszystkie media z tego albumu
    medias = list(instance.media.all())
    
    for media in medias:
        if not media:
            continue
        
        # Sprawdź, czy media jest tylko w tym albumie
        # (nie licząc tego albumu, który właśnie usuwamy)
        other_albums_count = media.albums.exclude(id=instance.id).count()
        
        if other_albums_count == 0:
            # Media jest tylko w tym albumie - usuń je wraz z plikami
            # Usuń plik fizyczny
            if media.file:
                try:
                    if os.path.exists(media.file.path):
                        os.remove(media.file.path)
                except Exception:
                    pass  # Ignoruj błędy usuwania pliku
            
            # Usuń miniaturkę jeśli istnieje
            if media.thumbnail_path:
                try:
                    if os.path.exists(media.thumbnail_path):
                        os.remove(media.thumbnail_path)
                except Exception:
                    pass  # Ignoruj błędy usuwania miniaturki
            
            # Usuń obiekt Media (to automatycznie usunie powiązanie ManyToMany)
            media.delete()
