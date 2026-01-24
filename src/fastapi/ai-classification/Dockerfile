FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE 1
ENV PYTHONUNBUFFERED 1

# Instalacja zależności systemowych dla PyTorch
RUN apt-get update && apt-get install -y \
    libgomp1 \
    curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY requirements.txt .
# Instalacja PyTorch CPU (lżejsza wersja)
RUN pip install --no-cache-dir torch torchvision --index-url https://download.pytorch.org/whl/cpu
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

# Ustaw uprawnienia dla entrypoint.sh
RUN chmod +x entrypoint.sh

CMD ["./entrypoint.sh"]