from django.test import TestCase
from django.contrib.auth import get_user_model
from rest_framework.test import APIClient
from rest_framework import status

User = get_user_model()


class UserRegistrationTestCase(TestCase):
    """Testy dla rejestracji użytkowników"""
    
    def setUp(self):
        self.client = APIClient()
        self.register_url = '/api/accounts/register/'
    
    def test_user_registration_success(self):
        """Test pomyślnej rejestracji użytkownika"""
        data = {
            'email': 'test@example.com',
            'username': 'testuser',
            'password': 'TestPassword123!',
            'password_confirm': 'TestPassword123!',
            'first_name': 'Test',
            'last_name': 'User'
        }
        response = self.client.post(self.register_url, data, format='json')
        self.assertEqual(response.status_code, status.HTTP_201_CREATED)
        self.assertIn('access', response.data)
        self.assertIn('refresh', response.data)
        self.assertIn('user', response.data)
    
    def test_user_registration_password_mismatch(self):
        """Test rejestracji z niezgodnymi hasłami"""
        data = {
            'email': 'test@example.com',
            'username': 'testuser',
            'password': 'TestPassword123!',
            'password_confirm': 'DifferentPassword123!',
        }
        response = self.client.post(self.register_url, data, format='json')
        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)


class UserLoginTestCase(TestCase):
    """Testy dla logowania użytkowników"""
    
    def setUp(self):
        self.client = APIClient()
        self.login_url = '/api/accounts/login/'
        self.user = User.objects.create_user(
            email='test@example.com',
            username='testuser',
            password='TestPassword123!'
        )
    
    def test_user_login_success(self):
        """Test pomyślnego logowania"""
        data = {
            'email': 'test@example.com',
            'password': 'TestPassword123!'
        }
        response = self.client.post(self.login_url, data, format='json')
        self.assertEqual(response.status_code, status.HTTP_200_OK)
        self.assertIn('access', response.data)
        self.assertIn('refresh', response.data)
        self.assertIn('user', response.data)
    
    def test_user_login_invalid_credentials(self):
        """Test logowania z nieprawidłowymi danymi"""
        data = {
            'email': 'test@example.com',
            'password': 'WrongPassword123!'
        }
        response = self.client.post(self.login_url, data, format='json')
        self.assertEqual(response.status_code, status.HTTP_400_BAD_REQUEST)