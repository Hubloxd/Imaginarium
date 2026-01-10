import base64
import logging

from fastapi import FastAPI, HTTPException
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from classifier import get_classifier

# Konfiguracja logowania
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

app = FastAPI(
    title="AI Classification Service",
    description="Mikroserwis do klasyfikacji obrazów na kategorie: zwierzę, człowiek, krajobraz, budynek",
    version="1.0.0"
)


class ClassifyRequest(BaseModel):
    """Request model do klasyfikacji obrazu."""
    image_base64: str
    media_id: str = None


@app.get("/health")
def health():
    """Health check endpoint dla Docker healthcheck"""
    return {"status": "ok", "service": "ai-classification"}


@app.post("/classify")
async def classify_image(request: ClassifyRequest):
    """
    Klasyfikuje przesłany obraz synchronicznie i zwraca wynik bezpośrednio.
    
    Args:
        request: Request z obrazem w base64 i opcjonalnym media_id
        
    Returns:
        JSON z kategorią, pewnością i wszystkimi wynikami
    """
    try:
        # Walidacja obrazu base64
        try:
            image_bytes = base64.b64decode(request.image_base64)
        except Exception as e:
            raise HTTPException(
                status_code=400,
                detail=f"Nieprawidłowy format base64: {str(e)}"
            )
        
        if len(image_bytes) == 0:
            raise HTTPException(
                status_code=400,
                detail="Obraz jest pusty"
            )
        
        max_size = 30 * 1024 * 1024  # 30MB
        if len(image_bytes) > max_size:
            raise HTTPException(
                status_code=400,
                detail=f"Obraz jest za duży. Maksymalny rozmiar: {max_size / 1024 / 1024}MB"
            )
        
        logger.info(f"Klasyfikacja obrazu: media_id={request.media_id}, rozmiar: {len(image_bytes)} bytes")
        
        # Klasyfikuj obraz
        classifier = get_classifier()
        result = classifier.classify(image_bytes)
        
        logger.info(f"Wynik klasyfikacji: kategoria={result['category']}, pewność={result['confidence']}, media_id={request.media_id}")
        
        return JSONResponse(content={
            "success": True,
            "media_id": request.media_id,
            "category": result["category"],
            "confidence": result["confidence"],
            "predictions": result["predictions"]
        })
        
    except HTTPException:
        raise
    except ValueError as e:
        logger.error(f"Błąd walidacji: {e}")
        raise HTTPException(status_code=400, detail=str(e))
    except Exception as e:
        logger.error(f"Nieoczekiwany błąd podczas klasyfikacji: {e}", exc_info=True)
        raise HTTPException(
            status_code=500,
            detail=f"Wystąpił błąd podczas klasyfikacji obrazu: {str(e)}"
        )
