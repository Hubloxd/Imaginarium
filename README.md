# Imaginarium

Aplikacja Imaginarium to galeria zdjęć i filmów, stworzona z myślą o użytkownikach, którzy chcą przechowywać, organizować i udostępniać swoje multimedialne wspomnienia w bezpieczny sposób na własnej infrastrukturze. Celem projektu jest zbudowanie funkcjonalnej platformy umożliwiającej zarządzanie kolekcją zdjęć i filmów z automatyczną klasyfikacją treści oraz elastycznym systemem udostępniania.

## Zrzuty ekranu

![Screenshot 1](screenshots/screenshot1.png)

![Screenshot 2](screenshots/screenshot2.png)

![Screenshot 3](screenshots/screenshot3.png)

![Screenshot 4](screenshots/screenshot4.png)

## Główne funkcjonalności

- **System użytkowników** – rejestracja i logowanie użytkowników z autoryzacją i uwierzytelnianiem
- **Zarządzanie multimediami** – przesyłanie, przeglądanie, edycja i usuwanie zdjęć oraz filmów
- **Albumy** – tworzenie i organizowanie albumów tematycznych ze zdjęciami i filmami
- **Udostępnianie treści** – możliwość udostępniania albumów i pojedynczych plików innym użytkownikom (zarówno zarejestrowanym, jak i poprzez linki publiczne)
- **Automatyczne tagowanie** – klasyfikacja zdjęć przez sztuczną inteligencję (np. krajobraz, zwierzę, osoba, budynek)
- **Wyszukiwanie** – zaawansowane wyszukiwanie po tagach, datach, albumach i metadanych
- **Podgląd i miniaturki** – automatyczne generowanie miniatur i optymalizacja wyświetlania

## Architektura

### Wzorzec architektoniczny
Mikroserwisy z separacją front-end i back-end

### Stack technologiczny

1. **Backend**: .NET (ASP.NET Core Web API)
2. **Frontend**: Angular + Tailwind CSS
3. **Baza danych**: PostgreSQL
4. **Mikroserwis AI**: Serwis klasyfikujący z wykorzystaniem modeli ML (TensorFlow, PyTorch) - FastAPI
5. **Kolejka zadań**: Redis dla asynchronicznego przetwarzania multimediów
6. **Kontenery**: Docker + Docker Compose dla łatwego wdrożenia
7. **Reverse Proxy**: Nginx

## Struktura projektu

```
ImaginariumBackend/
├── deployment/              # Konfiguracja Docker Compose i Nginx
│   ├── docker-compose.yml
│   └── nginx.conf
├── src/
│   ├── dotnet/
│   │   └── imaginarium-backend/    # Backend API (.NET)
│   ├── angular/
│   │   └── imaginarium-front/      # Frontend (Angular)
│   └── fastapi/
│       └── ai-classification/      # Mikroserwis AI (FastAPI)
└── README.md
```

## Wymagania

- Docker i Docker Compose
- Git

## Instalacja i uruchomienie

1. Sklonuj repozytorium:
```bash
git clone <repository-url>
cd ImaginariumBackend
```

2. Przejdź do katalogu deployment:
```bash
cd deployment
```

3. Utwórz pliki konfiguracyjne środowiska:
   - `env/imaginarium.env` - konfiguracja backendu
   - `env/postgres.env` - konfiguracja bazy danych PostgreSQL

4. Uruchom aplikację za pomocą Docker Compose:
```bash
docker-compose up -d
```

## Dostęp do serwisów

Po uruchomieniu aplikacja będzie dostępna pod następującymi adresami:

- **Frontend**: http://localhost:8080
- **Backend API**: http://localhost:5000
- **Mikroserwis AI**: http://localhost:8000
- **Nginx (Reverse Proxy)**: http://localhost:4444
- **PgAdmin**: http://localhost:8081

## Konfiguracja

### Zmienne środowiskowe

Aplikacja wymaga skonfigurowania następujących plików środowiskowych w katalogu `deployment/env/`:

- `imaginarium.env` - konfiguracja aplikacji backend (np. connection strings, secrets)
- `postgres.env` - konfiguracja bazy danych PostgreSQL (POSTGRES_USER, POSTGRES_PASSWORD, POSTGRES_DB)

### Domyślne dane dostępowe PgAdmin

- Email: `admin@admin.com`
- Hasło: `admin`

## Zatrzymywanie aplikacji

Aby zatrzymać wszystkie kontenery:
```bash
docker-compose down
```

Aby zatrzymać i usunąć wolumeny (uwaga: spowoduje to usunięcie danych):
```bash
docker-compose down -v
```

## Dokumentacja API

### Zarządzanie kontami

- **POST** `/api/accounts/register` - Rejestracja nowego użytkownika
  - Body: `RegisterDto` (email, username, password)
  - Zwraca: `AuthResponseDto` (id, email, username, accessToken)

- **POST** `/api/accounts/login` - Logowanie użytkownika
  - Body: `LoginDto` (email, password)
  - Zwraca: `AuthResponseDto` (id, email, username, accessToken)

### Zarządzanie albumami

- **POST** `/api/albums` - Utworzenie nowego albumu
  - Body: `multipart/form-data` (Name, Description, files[])
  - Zwraca: `AlbumResponseDto`

- **GET** `/api/albums` - Pobranie listy wszystkich albumów
  - Zwraca: `AlbumResponseDto[]`

- **GET** `/api/albums/{id}` - Pobranie szczegółów albumu
  - Parametry: `id` (UUID)
  - Zwraca: `AlbumDetailResponseDto`

- **DELETE** `/api/albums/{id}` - Usunięcie albumu
  - Parametry: `id` (UUID)

- **POST** `/api/albums/{albumId}/media` - Dodanie mediów do albumu
  - Parametry: `albumId` (UUID)
  - Body: `multipart/form-data` (files[])
  - Zwraca: `AlbumDetailResponseDto`

- **POST** `/api/albums/{albumId}/media/{mediaId}` - Dodanie istniejącego medium do albumu
  - Parametry: `albumId` (UUID), `mediaId` (UUID)

- **DELETE** `/api/albums/{albumId}/media/{mediaId}` - Usunięcie medium z albumu
  - Parametry: `albumId` (UUID), `mediaId` (UUID)

### Zarządzanie grupami

- **POST** `/api/groups` - Utworzenie nowej grupy
  - Body: `CreateGroupDto` (name, description, isPrivate)
  - Zwraca: `GroupResponseDto`

- **GET** `/api/groups` - Pobranie listy wszystkich grup
  - Zwraca: `GroupResponseDto[]`

- **GET** `/api/groups/{id}` - Pobranie szczegółów grupy
  - Parametry: `id` (UUID)
  - Zwraca: `GroupResponseDto`

- **DELETE** `/api/groups/{id}` - Usunięcie grupy
  - Parametry: `id` (UUID)

- **GET** `/api/groups/invitations` - Pobranie listy zaproszeń do grup
  - Zwraca: `GroupMemberDto[]`

- **POST** `/api/groups/{groupId}/invite` - Zaproszenie użytkownika do grupy
  - Parametry: `groupId` (UUID)
  - Body: `InviteUserDto` (email, role)

- **POST** `/api/groups/{groupId}/accept` - Akceptacja zaproszenia do grupy
  - Parametry: `groupId` (UUID)

- **POST** `/api/groups/{groupId}/reject` - Odrzucenie zaproszenia do grupy
  - Parametry: `groupId` (UUID)

- **GET** `/api/groups/{groupId}/members` - Pobranie listy członków grupy
  - Parametry: `groupId` (UUID)
  - Zwraca: `GroupMemberDto[]`

- **DELETE** `/api/groups/{groupId}/members/{memberId}` - Usunięcie członka z grupy
  - Parametry: `groupId` (UUID), `memberId` (UUID)

- **PUT** `/api/groups/{groupId}/members/{memberId}/role` - Aktualizacja roli członka
  - Parametry: `groupId` (UUID), `memberId` (UUID)
  - Body: `UpdateMemberRoleDto` (role)

- **POST** `/api/groups/{groupId}/leave` - Opuszczenie grupy
  - Parametry: `groupId` (UUID)

### Zarządzanie mediami

- **GET** `/api/media` - Pobranie listy wszystkich mediów
  - Zwraca: `MediaResponseDto[]`

- **GET** `/api/media/search` - Wyszukiwanie mediów
  - Query params: `query` (string), `tag` (string), `date` (date-time), `albumId` (UUID)
  - Zwraca: `MediaResponseDto[]`

- **GET** `/api/media/{id}` - Pobranie szczegółów medium
  - Parametry: `id` (UUID)
  - Zwraca: `MediaResponseDto`

- **PUT** `/api/media/{id}` - Aktualizacja medium
  - Parametry: `id` (UUID)
  - Body: `multipart/form-data` (file)
  - Zwraca: `MediaResponseDto`

- **DELETE** `/api/media/{id}` - Usunięcie medium
  - Parametry: `id` (UUID)

### Zarządzanie udostępnieniami

- **POST** `/api/shares` - Utworzenie udostępnienia
  - Body: `CreateShareDto` (mediaId, albumId, sharedWithUserId, sharedWithUserEmail, sharedWithGroupId, isPublic, expiresAt, permissionLevel)
  - Zwraca: `ShareResponseDto`

- **GET** `/api/shares/token/{token}` - Pobranie udostępnienia po tokenie
  - Parametry: `token` (string)
  - Zwraca: `ShareResponseDto`

- **GET** `/api/shares/validate/{token}` - Walidacja tokenu udostępnienia
  - Parametry: `token` (string)
  - Zwraca: `boolean`

- **DELETE** `/api/shares/{id}` - Usunięcie udostępnienia
  - Parametry: `id` (UUID)

- **POST** `/api/shares/group` - Utworzenie udostępnienia dla grupy
  - Body: `CreateShareDto`
  - Zwraca: `ShareResponseDto`

- **GET** `/api/shares/group/{groupId}` - Pobranie udostępnień grupy
  - Parametry: `groupId` (UUID)
  - Zwraca: `ShareResponseDto[]`

- **GET** `/api/shares/for-me` - Pobranie udostępnień otrzymanych przez użytkownika
  - Zwraca: `ShareResponseDto[]`

- **GET** `/api/shares/by-me` - Pobranie udostępnień utworzonych przez użytkownika
  - Zwraca: `ShareResponseDto[]`

### Uwagi

- Wszystkie endpointy wymagają autoryzacji przez token Bearer (JWT), z wyjątkiem endpointów rejestracji i logowania

## Rozwój

### Backend (.NET)
Kod backendu znajduje się w `src/dotnet/imaginarium-backend/`

### Frontend (Angular)
Kod frontendu znajduje się w `src/angular/imaginarium-front/`

### Mikroserwis AI (FastAPI)
Kod mikroserwisu AI znajduje się w `src/fastapi/ai-classification/`

