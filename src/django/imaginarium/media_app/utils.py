from __future__ import annotations

import logging

logger = logging.getLogger(__name__)


def safe_delay(task, *args, **kwargs):
    """
    Próbuje wywołać task asynchronicznie przez Celery.
    Jeśli Celery nie jest dostępny (np. Redis nie działa), wywołuje task synchronicznie.
    """
    try:
        return task.delay(*args, **kwargs)
    except Exception as e:
        logger.warning(
            f"Nie można wywołać taska {task.name} asynchronicznie (Celery niedostępny): {e}. "
            "Wywoływanie synchronicznie..."
        )
        try:
            return task(*args, **kwargs)
        except Exception as sync_error:
            logger.error(f"Błąd podczas synchronicznego wywołania taska {task.name}: {sync_error}")
            return None
