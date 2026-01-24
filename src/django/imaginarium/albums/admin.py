from django.contrib import admin

from .models import Album


@admin.register(Album)
class AlbumAdmin(admin.ModelAdmin):
    list_display = ("id", "user", "name", "created_at", "updated_at", "cover_media")
    search_fields = ("name", "user__email", "user__username")

