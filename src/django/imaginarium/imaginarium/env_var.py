import os

DB_NAME = os.getenv('DB_NAME', 'Nazwa bazy danych')
DB_USER = os.getenv('DB_USER', 'Użytkownik bazy danych')
DB_PASS = os.getenv('DB_PASS', 'Hasło do bazy danych')
DB_HOST = os.getenv('DB_HOST', 'Host bazy danych')
DB_PORT = os.getenv('DB_PORT', 'Port bazy danych')

CELERY_BROKER_REDIS_URL = os.getenv('CELERY_BROKER_REDIS_URL', 'URL brokera Celery')
CACHE_REDIS_URL = os.getenv('CACHE_REDIS_URL', 'URL Redis Cache')
