from django.urls import path

from .views import (
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

urlpatterns = [
    path("", GroupListCreateView.as_view(), name="groups"),
    path("invitations", GroupInvitationsView.as_view(), name="group-invitations"),
    path("<uuid:group_id>", GroupDetailView.as_view(), name="group-detail"),
    path("<uuid:group_id>/invite", GroupInviteUserView.as_view(), name="group-invite"),
    path("<uuid:group_id>/accept", GroupAcceptInvitationView.as_view(), name="group-accept"),
    path("<uuid:group_id>/reject", GroupRejectInvitationView.as_view(), name="group-reject"),
    path("<uuid:group_id>/members", GroupMembersView.as_view(), name="group-members"),
    path("<uuid:group_id>/members/<str:member_id>", GroupRemoveMemberView.as_view(), name="group-remove-member"),
    path(
        "<uuid:group_id>/members/<str:member_id>/role",
        GroupUpdateMemberRoleView.as_view(),
        name="group-update-role",
    ),
    path("<uuid:group_id>/leave", GroupLeaveView.as_view(), name="group-leave"),
]

