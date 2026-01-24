from django.contrib import admin

from .models import Group, GroupMember


@admin.register(Group)
class GroupAdmin(admin.ModelAdmin):
    list_display = ("id", "name", "created_by", "is_private", "created_at")
    search_fields = ("name", "created_by__email", "created_by__username")


@admin.register(GroupMember)
class GroupMemberAdmin(admin.ModelAdmin):
    list_display = ("id", "group", "user", "role", "joined_at", "invited_by")
    search_fields = ("group__name", "user__email", "user__username")

