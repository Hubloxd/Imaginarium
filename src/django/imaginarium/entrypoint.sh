#!/usr/bin/env bash
python3 manage.py collectstatic --noinput

# Start Celery worker
celery -A imaginarium worker -Q imaginarium --loglevel=info --concurrency=2 &
celery -A imaginarium beat -l info --scheduler django_celery_beat.schedulers:DatabaseScheduler &

python3 manage.py migrate

ln -s staticfiles static
#python3 -m http.server 9000 &

# Start Gunicorn
gunicorn --config gunicorn-cfg.py imaginarium.wsgi