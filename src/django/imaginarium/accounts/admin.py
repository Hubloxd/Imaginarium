from django.contrib import admin
from django.contrib.auth.admin import UserAdmin as BaseUserAdmin
from .models import User


@admin.register(User)
class UserAdmin(BaseUserAdmin):
    """Konfiguracja panelu administracyjnego dla modelu User"""
    list_display = ['email', 'username', 'first_name', 'last_name', 
                    'is_staff', 'is_active', 'is_email_verified', 'created_at']
    list_filter = ['is_staff', 'is_active', 'is_email_verified', 'created_at']
    search_fields = ['email', 'username', 'first_name', 'last_name']
    ordering = ['-created_at']
    
    fieldsets = BaseUserAdmin.fieldsets + (
        ('Dodatkowe informacje', {
            'fields': ('avatar', 'bio', 'is_email_verified')
        }),
    )