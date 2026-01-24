import os
from celery import Celery
from celery.schedules import crontab

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'imaginarium.settings')

app = Celery('imaginarium')

app.config_from_object('django.conf:settings', namespace='CELERY')

app.conf.task_acks_late = True
app.conf.broker_connection_retry_on_startup = True
app.conf.broker_connection_retry = True
app.conf.broker_connection_max_retries = 10

app.conf.beat_schedule = {
}

# Automatyczne wykrywanie tasków we wszystkich zainstalowanych aplikacjach Django
app.autodiscover_tasks()
