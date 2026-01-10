#!/bin/bash

# Uruchomienie Celery workera w tle
celery -A worker worker --loglevel=info --concurrency=2 &

# Uruchomienie aplikacji FastAPI za pomocą uvicorn
exec uvicorn main:app --host 0.0.0.0 --port 8000