import base64
import logging
from celery import Celery
from shared import set_status
from classifier import get_classifier

logger = logging.getLogger(__name__)

# Konfiguracja logowania
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)

celery = Celery(
    "ai-classification-worker",
    broker="redis://redis:6379/0",
    backend="redis://redis:6379/1",
)

celery.conf.update(
    task_serializer='json',
    accept_content=['json'],
    result_serializer='json',
    timezone='UTC',
    enable_utc=True,
)


@celery.task(name="classify_image")
def classify_image_task(job_id: str, image_base64: str, media_id: str = None):
    """
    Task Celery do klasyfikacji obrazu.
    
    Args:
        job_id: Unikalny identyfikator zadania
        image_base64: Obraz zakodowany w base64
        media_id: Opcjonalny identyfikator media z backendu
        
    Returns:
        Dict z wynikami klasyfikacji
    """
    try:
        logger.info(f"Rozpoczęcie klasyfikacji dla job_id: {job_id}, media_id: {media_id}")
        
        # Ustaw status: processing
        set_status(job_id, {
            "status": "processing",
            "job_id": job_id,
            "media_id": media_id
        })
        
        # Dekoduj obraz z base64
        image_bytes = base64.b64decode(image_base64)
        
        # Klasyfikuj obraz
        classifier = get_classifier()
        result = classifier.classify(image_bytes)
        
        # Przygotuj wynik
        classification_result = {
            "status": "completed",
            "job_id": job_id,
            "media_id": media_id,
            "category": result["category"],
            "confidence": result["confidence"],
            "all_scores": result["all_scores"]
        }
        
        # Zapisz wynik w Redis
        set_status(job_id, classification_result)
        
        logger.info(f"Klasyfikacja zakończona dla job_id: {job_id}, kategoria: {result['category']}")
        
        return classification_result
        
    except Exception as e:
        logger.error(f"Błąd podczas klasyfikacji dla job_id: {job_id}: {e}", exc_info=True)
        
        # Zapisz błąd w Redis
        error_result = {
            "status": "error",
            "job_id": job_id,
            "media_id": media_id,
            "error": str(e)
        }
        set_status(job_id, error_result)
        
        raise
