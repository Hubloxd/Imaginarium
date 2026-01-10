from django.contrib.auth.models import AbstractUser
from django.db import models


class User(AbstractUser):
    """
    Rozszerzony model użytkownika dla aplikacji Imaginarium.
    Dziedziczy po AbstractUser, co daje wszystkie standardowe pola Django User.
    """
    email = models.EmailField(
        unique=True,
        verbose_name='Email',
        help_text='Adres email użytkownika (wymagany)'
    )
    avatar = models.ImageField(
        upload_to='avatars/',
        null=True,
        blank=True,
        verbose_name='Avatar',
        help_text='Zdjęcie profilowe użytkownika'
    )
    bio = models.TextField(
        max_length=500,
        blank=True,
        verbose_name='Biografia',
        help_text='Krótki opis użytkownika'
    )
    created_at = models.DateTimeField(
        auto_now_add=True,
        verbose_name='Data utworzenia'
    )
    updated_at = models.DateTimeField(
        auto_now=True,
        verbose_name='Data aktualizacji'
    )
    is_email_verified = models.BooleanField(
        default=False,
        verbose_name='Email zweryfikowany',
        help_text='Czy adres email został zweryfikowany'
    )

    USERNAME_FIELD = 'email'
    REQUIRED_FIELDS = ['username']

    class Meta:
        verbose_name = 'Użytkownik'
        verbose_name_plural = 'Użytkownicy'
        ordering = ['-created_at']

    def __str__(self):
        return self.email or self.username