from django.urls import path

from .views import MediaDetailView, MediaListView, MediaSearchView

urlpatterns = [
    path("", MediaListView.as_view(), name="media-list"),
    path("search", MediaSearchView.as_view(), name="media-search"),
    path("<uuid:media_id>", MediaDetailView.as_view(), name="media-detail"),
]

