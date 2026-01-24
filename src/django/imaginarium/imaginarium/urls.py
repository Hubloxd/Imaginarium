"""URL configuration for imaginarium project."""

from django.conf import settings
from django.conf.urls.static import static
from django.contrib import admin
from django.urls import include, path
from drf_spectacular.views import (
    SpectacularAPIView,
    SpectacularRedocView,
    SpectacularSwaggerView,
)

from albums.views import AlbumAddFilesView, AlbumDetailView, AlbumListCreateView, AlbumMediaLinkView
from groups.views import (
    GroupAcceptInvitationView,
    GroupDetailView,
    GroupInvitationsView,
    GroupInviteUserView,
    GroupLeaveView,
    GroupListCreateView,
    GroupMembersView,
    GroupRejectInvitationView,
    GroupRemoveMemberView,
    GroupUpdateMemberRoleView,
)
from media_app.views import MediaDetailView, MediaListView, MediaSearchView
from sharing.views import (
    GroupSharesView,
    ShareByTokenView,
    ShareContentByTokenView,
    ShareCreateView,
    ShareRevokeView,
    ShareValidateView,
    ShareWithGroupView,
    SharesByMeView,
    SharesForMeView,
)

urlpatterns = [
    path("admin/", admin.site.urls),
    
    # OpenAPI Schema
    path("api/schema/", SpectacularAPIView.as_view(), name="schema"),
    path("api/schema/swagger-ui/", SpectacularSwaggerView.as_view(url_name="schema"), name="swagger-ui"),
    path("api/schema/redoc/", SpectacularRedocView.as_view(url_name="schema"), name="redoc"),
    path("api/schema/openapi.json", SpectacularAPIView.as_view(), name="openapi"),
    
    path("api/accounts/", include("accounts.urls")),

    # Albums
    path("api/albums", AlbumListCreateView.as_view()),
    path("api/albums/<uuid:album_id>", AlbumDetailView.as_view()),
    path("api/albums/<uuid:album_id>/media", AlbumAddFilesView.as_view()),
    path("api/albums/<uuid:album_id>/media/<uuid:media_id>", AlbumMediaLinkView.as_view()),

    # Media
    path("api/media", MediaListView.as_view()),
    path("api/media/search", MediaSearchView.as_view()),
    path("api/media/<uuid:media_id>", MediaDetailView.as_view()),

    # Groups
    path("api/groups", GroupListCreateView.as_view()),
    path("api/groups/invitations", GroupInvitationsView.as_view()),
    path("api/groups/<uuid:group_id>", GroupDetailView.as_view()),
    path("api/groups/<uuid:group_id>/invite", GroupInviteUserView.as_view()),
    path("api/groups/<uuid:group_id>/accept", GroupAcceptInvitationView.as_view()),
    path("api/groups/<uuid:group_id>/reject", GroupRejectInvitationView.as_view()),
    path("api/groups/<uuid:group_id>/members", GroupMembersView.as_view()),
    path("api/groups/<uuid:group_id>/members/<str:member_id>", GroupRemoveMemberView.as_view()),
    path("api/groups/<uuid:group_id>/members/<str:member_id>/role", GroupUpdateMemberRoleView.as_view()),
    path("api/groups/<uuid:group_id>/leave", GroupLeaveView.as_view()),

    # Shares
    path("api/shares", ShareCreateView.as_view()),
    path("api/shares/token/<str:token>", ShareByTokenView.as_view()),
    path("api/shares/token/<str:token>/content", ShareContentByTokenView.as_view()),
    path("api/shares/validate/<str:token>", ShareValidateView.as_view()),
    path("api/shares/<uuid:share_id>", ShareRevokeView.as_view()),
    path("api/shares/group", ShareWithGroupView.as_view()),
    path("api/shares/group/<uuid:group_id>", GroupSharesView.as_view()),
    path("api/shares/for-me", SharesForMeView.as_view()),
    path("api/shares/by-me", SharesByMeView.as_view()),
]

if settings.DEBUG:
    urlpatterns += static(settings.MEDIA_URL, document_root=settings.MEDIA_ROOT)
    urlpatterns += static(settings.THUMBNAILS_URL, document_root=settings.THUMBNAILS_ROOT)
    urlpatterns += static(settings.STATIC_URL, document_root=settings.STATIC_ROOT)
