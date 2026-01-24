from django.contrib import admin

from .models import Media


@admin.register(Media)
class MediaAdmin(admin.ModelAdmin):
    list_display = ("id", "user", "file_name", "media_type", "mime_type", "uploaded_at")
    search_fields = ("file_name", "mime_type", "user__email", "user__username")
    list_filter = ("media_type",)

