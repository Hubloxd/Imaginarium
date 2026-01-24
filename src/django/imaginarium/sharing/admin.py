from django.contrib import admin

from .models import Share


@admin.register(Share)
class ShareAdmin(admin.ModelAdmin):
    list_display = (
        "id",
        "share_token",
        "is_public",
        "permission_level",
        "shared_by_user",
        "shared_with_user",
        "shared_with_group",
        "created_at",
        "expires_at",
    )
    search_fields = ("share_token", "shared_by_user__email", "shared_with_user__email")
    list_filter = ("is_public", "permission_level")

