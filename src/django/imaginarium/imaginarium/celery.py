import os
from celery import Celery
from celery.schedules import crontab

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'imaginarium.settings')

app = Celery('imaginarium')

app.config_from_object('django.conf:settings', namespace='CELERY')

app.conf.task_acks_late = True

app.conf.beat_schedule = {
}

app.autodiscover_tasks()
