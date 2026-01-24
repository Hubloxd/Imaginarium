from django.contrib import admin

from .models import Tag


@admin.register(Tag)
class TagAdmin(admin.ModelAdmin):
    list_display = ("id", "name", "category", "confidence")
    search_fields = ("name", "category")
    list_filter = ("category",)

