from django.urls import path

from .views import (
    AlbumAddFilesView,
    AlbumDetailView,
    AlbumListCreateView,
    AlbumMediaLinkView,
)

urlpatterns = [
    path("", AlbumListCreateView.as_view(), name="albums"),
    path("<uuid:album_id>", AlbumDetailView.as_view(), name="album-detail"),
    path("<uuid:album_id>/media", AlbumAddFilesView.as_view(), name="album-add-files"),
    path("<uuid:album_id>/media/<uuid:media_id>", AlbumMediaLinkView.as_view(), name="album-media-link"),
]

