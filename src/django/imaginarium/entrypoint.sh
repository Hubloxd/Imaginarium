#!/usr/bin/env bash
set -e

python3 manage.py migrate --noinput
python3 manage.py collectstatic --noinput

mkdir -p media thumbnails staticfiles

ln -sf staticfiles static
#python3 -m http.server 9000 &

# Wait for Redis to be ready
echo "Waiting for Redis to be ready..."
until python3 -c "import redis; import os; url = os.getenv('CELERY_BROKER_REDIS_URL', 'redis://redis:6379/0'); r = redis.from_url(url); r.ping()" 2>/dev/null; do
  echo "Redis is unavailable - sleeping"
  sleep 1
done

echo "Redis is ready - starting Celery workers"

# Start Celery worker
celery -A imaginarium worker -Q imaginarium --loglevel=info --concurrency=2 &
celery -A imaginarium beat -l info --scheduler django_celery_beat.schedulers:DatabaseScheduler &

# Start Gunicorn
gunicorn --config gunicorn-cfg.py imaginarium.wsgi