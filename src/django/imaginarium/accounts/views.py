from rest_framework import status, generics, permissions
from rest_framework.decorators import api_view, permission_classes
from rest_framework.response import Response
from rest_framework_simplejwt.tokens import RefreshToken
from django.contrib.auth import login
from .models import User
from .serializers import (
    UserRegistrationSerializer,
    UserSerializer,
    UserUpdateSerializer,
    LoginSerializer,
    ChangePasswordSerializer
)


class UserRegistrationView(generics.CreateAPIView):
    """
    Endpoint do rejestracji nowych użytkowników.
    POST /api/accounts/register/
    """
    queryset = User.objects.all()
    serializer_class = UserRegistrationSerializer
    permission_classes = [permissions.AllowAny]

    def create(self, request, *args, **kwargs):
        serializer = self.get_serializer(data=request.data)
        serializer.is_valid(raise_exception=True)
        user = serializer.save()
        
        # Tworzenie tokenów JWT dla nowego użytkownika
        refresh = RefreshToken.for_user(user)
        
        return Response(
            {
                'id': str(user.pk),
                'email': user.email,
                'username': user.username,
                'accessToken': str(refresh.access_token),
                'message': 'Rejestracja zakończona sukcesem.',
            },
            status=status.HTTP_200_OK,
        )


@api_view(['POST'])
@permission_classes([permissions.AllowAny])
def login_view(request):
    """
    Endpoint do logowania użytkowników.
    POST /api/accounts/login/
    CSRF jest wyłączony przez middleware dla wszystkich /api/ endpointów.
    """
    serializer = LoginSerializer(data=request.data, context={'request': request})
    serializer.is_valid(raise_exception=True)
    user = serializer.validated_data['user']
    
    # Logowanie użytkownika (opcjonalne, dla sesji)
    # login(request, user)
    
    # Tworzenie tokenów JWT
    refresh = RefreshToken.for_user(user)
    
    return Response(
        {
            'id': str(user.pk),
            'email': user.email,
            'username': user.username,
            'accessToken': str(refresh.access_token),
            'message': 'Logowanie zakończone sukcesem.',
        },
        status=status.HTTP_200_OK,
    )


@api_view(['POST'])
@permission_classes([permissions.IsAuthenticated])
def logout_view(request):
    """
    Endpoint do wylogowania użytkownika.
    POST /api/accounts/logout/
    Opcjonalnie można przesłać refresh token w body: {"refresh": "..."}
    Uwaga: Bez blacklist tokeny pozostaną ważne do wygaśnięcia.
    """
    # W przyszłości można dodać blacklist tokenów
    # Wymaga to dodania 'rest_framework_simplejwt.token_blacklist' do INSTALLED_APPS
    return Response({
        'message': 'Wylogowanie zakończone sukcesem.'
    }, status=status.HTTP_200_OK)


class UserProfileView(generics.RetrieveUpdateAPIView):
    """
    Endpoint do pobierania i aktualizacji profilu użytkownika.
    GET /api/accounts/profile/ - pobierz profil
    PUT /api/accounts/profile/ - zaktualizuj profil
    PATCH /api/accounts/profile/ - częściowa aktualizacja profilu
    """
    serializer_class = UserSerializer
    permission_classes = [permissions.IsAuthenticated]

    def get_object(self):
        return self.request.user

    def get_serializer_class(self):
        if self.request.method in ['PUT', 'PATCH']:
            return UserUpdateSerializer
        return UserSerializer


class ChangePasswordView(generics.UpdateAPIView):
    """
    Endpoint do zmiany hasła użytkownika.
    PUT /api/accounts/change-password/
    """
    serializer_class = ChangePasswordSerializer
    permission_classes = [permissions.IsAuthenticated]

    def update(self, request, *args, **kwargs):
        serializer = self.get_serializer(data=request.data)
        serializer.is_valid(raise_exception=True)
        serializer.save()
        
        return Response({
            'message': 'Hasło zostało zmienione pomyślnie.'
        }, status=status.HTTP_200_OK)