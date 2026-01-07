FROM python:3.13-slim
LABEL authors="huber"

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /code

RUN apt-get update && apt-get install -y --no-install-recommends \
    bash \
    build-essential \
    python3-dev \
    libpq-dev \
    postgresql-client \
    cron \
    curl \
    gnupg \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && python3 -m pip install --no-cache-dir --upgrade pip

COPY requirements.txt .
RUN python3 -m pip install --no-cache-dir -r requirements.txt
COPY . .

EXPOSE 8000

RUN chmod +x ./entrypoint.sh
#RUN chmod +x ./entrypoint_celery.sh

ENTRYPOINT ["bash", "entrypoint.sh"]