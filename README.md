# Imaginarium

Aplikacja Imaginarium to galeria zdjęć i filmów, stworzona z myślą o użytkownikach, którzy chcą przechowywać, organizować i udostępniać swoje multimedialne wspomnienia w bezpieczny sposób na własnej infrastrukturze. Celem projektu jest zbudowanie funkcjonalnej platformy umożliwiającej zarządzanie kolekcją zdjęć i filmów z automatyczną klasyfikacją treści oraz elastycznym systemem udostępniania.

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

## Rozwój

### Backend (.NET)
Kod backendu znajduje się w `src/dotnet/imaginarium-backend/`

### Frontend (Angular)
Kod frontendu znajduje się w `src/angular/imaginarium-front/`

### Mikroserwis AI (FastAPI)
Kod mikroserwisu AI znajduje się w `src/fastapi/ai-classification/`

