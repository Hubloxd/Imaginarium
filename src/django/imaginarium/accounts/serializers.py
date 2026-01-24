from rest_framework import serializers
from django.contrib.auth import authenticate
from django.contrib.auth.password_validation import validate_password
from .models import User


class UserRegistrationSerializer(serializers.ModelSerializer):
    """Serializer do rejestracji nowych użytkowników"""
    password = serializers.CharField(
        write_only=True,
        required=True,
        validators=[validate_password],
        style={'input_type': 'password'}
    )

    class Meta:
        model = User
        fields = ['email', 'username', 'password', 'first_name', 'last_name']
        extra_kwargs = {
            'email': {'required': True},
            'username': {'required': True},
        }

    def create(self, validated_data):
        """Tworzenie nowego użytkownika"""
        password = validated_data.pop('password')
        user = User.objects.create_user(**validated_data)
        user.set_password(password)
        user.save()
        return user


class UserSerializer(serializers.ModelSerializer):
    """Serializer do wyświetlania danych użytkownika"""
    class Meta:
        model = User
        fields = ['id', 'email', 'username', 'first_name', 'last_name',
                  'avatar', 'bio', 'is_email_verified', 'created_at', 'updated_at']
        read_only_fields = ['id', 'created_at', 'updated_at', 'is_email_verified']


class UserUpdateSerializer(serializers.ModelSerializer):
    """Serializer do aktualizacji danych użytkownika"""
    class Meta:
        model = User
        fields = ['first_name', 'last_name', 'avatar', 'bio']
        extra_kwargs = {
            'avatar': {'required': False},
            'bio': {'required': False},
        }


class LoginSerializer(serializers.Serializer):
    """Serializer do logowania użytkownika"""
    email = serializers.EmailField(required=True)
    password = serializers.CharField(
        write_only=True,
        required=True,
        style={'input_type': 'password'}
    )

    def validate(self, attrs):
        """Walidacja danych logowania"""
        email = attrs.get('email')
        password = attrs.get('password')

        if email and password:
            user = authenticate(request=self.context.get('request'),
                              username=email, password=password)
            if not user:
                raise serializers.ValidationError(
                    'Nieprawidłowy email lub hasło.',
                    code='authorization'
                )
            if not user.is_active:
                raise serializers.ValidationError(
                    'Konto użytkownika jest nieaktywne.',
                    code='authorization'
                )
            attrs['user'] = user
        else:
            raise serializers.ValidationError(
                'Musisz podać email i hasło.',
                code='authorization'
            )
        return attrs


class ChangePasswordSerializer(serializers.Serializer):
    """Serializer do zmiany hasła"""
    old_password = serializers.CharField(
        write_only=True,
        required=True,
        style={'input_type': 'password'}
    )
    new_password = serializers.CharField(
        write_only=True,
        required=True,
        validators=[validate_password],
        style={'input_type': 'password'}
    )
    new_password_confirm = serializers.CharField(
        write_only=True,
        required=True,
        style={'input_type': 'password'}
    )

    def validate(self, attrs):
        """Walidacja zgodności nowych haseł"""
        if attrs['new_password'] != attrs['new_password_confirm']:
            raise serializers.ValidationError({
                'new_password_confirm': 'Hasła nie są identyczne.'
            })
        return attrs

    def validate_old_password(self, value):
        """Walidacja starego hasła"""
        user = self.context['request'].user
        if not user.check_password(value):
            raise serializers.ValidationError('Stare hasło jest nieprawidłowe.')
        return value

    def save(self):
        """Zapisanie nowego hasła"""
        user = self.context['request'].user
        user.set_password(self.validated_data['new_password'])
        user.save()
        return user