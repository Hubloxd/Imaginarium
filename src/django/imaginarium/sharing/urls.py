from django.urls import path

from .views import (
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
    path("", ShareCreateView.as_view(), name="share-create"),
    path("token/<str:token>", ShareByTokenView.as_view(), name="share-by-token"),
    path("token/<str:token>/content", ShareContentByTokenView.as_view(), name="share-content-by-token"),
    path("validate/<str:token>", ShareValidateView.as_view(), name="share-validate"),
    path("<uuid:share_id>", ShareRevokeView.as_view(), name="share-revoke"),
    path("group", ShareWithGroupView.as_view(), name="share-with-group"),
    path("group/<uuid:group_id>", GroupSharesView.as_view(), name="group-shares"),
    path("for-me", SharesForMeView.as_view(), name="shares-for-me"),
    path("by-me", SharesByMeView.as_view(), name="shares-by-me"),
]

