import os

DB_NAME = os.getenv('DB_NAME', 'imaginarium')
DB_USER = os.getenv('DB_USER', 'imaginarium')
DB_PASS = os.getenv('DB_PASS', 'imaginarium')
DB_HOST = os.getenv('DB_HOST', 'postgres')
DB_PORT = os.getenv('DB_PORT', '5432')

CELERY_BROKER_REDIS_URL = os.getenv('CELERY_BROKER_REDIS_URL', 'redis://redis:6379/0')
CACHE_REDIS_URL = os.getenv('CACHE_REDIS_URL', 'redis://redis:6379/1')

AI_SERVICE_URL = os.getenv('AI_SERVICE_URL', 'http://ai-classification:8000/classify')
