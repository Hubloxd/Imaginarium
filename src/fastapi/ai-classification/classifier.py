"""
Moduł klasyfikacji obrazów używający EfficientNet-B0.
Zwraca bezpośrednie wyniki klasyfikacji ImageNet.
"""
import torch
import torch.nn as nn
from torchvision import transforms, models
from PIL import Image
import io
import logging
from typing import Dict, List, Tuple

logger = logging.getLogger(__name__)


class ImageClassifier:
    """Klasa do klasyfikacji obrazów używająca EfficientNet-B0."""
    
    def __init__(self):
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        logger.info(f"Używanie urządzenia: {self.device}")
        
        # Załaduj pre-trenowany EfficientNet-B0
        self.model = models.efficientnet_b0(weights=models.EfficientNet_B0_Weights.DEFAULT)
        self.model.eval()
        self.model.to(self.device)
        
        # Transformacje obrazu dla EfficientNet
        self.transform = transforms.Compose([
            transforms.Resize((224, 224)),
            transforms.ToTensor(),
            transforms.Normalize(
                mean=[0.485, 0.456, 0.406],
                std=[0.229, 0.224, 0.225]
            )
        ])
        
        # Załaduj etykiety ImageNet
        try:
            import urllib.request
            import os
            
            # Użyj TORCH_HOME jeśli jest ustawione, w przeciwnym razie domyślny cache
            torch_home = os.environ.get("TORCH_HOME", os.path.expanduser("~/.cache/torch"))
            os.makedirs(torch_home, exist_ok=True)
            
            imagenet_classes_path = os.path.join(torch_home, "imagenet_classes.txt")
            
            # Pobierz plik tylko jeśli nie istnieje
            if not os.path.exists(imagenet_classes_path):
                logger.info("Pobieranie imagenet_classes.txt...")
                url = "https://raw.githubusercontent.com/pytorch/hub/master/imagenet_classes.txt"
                urllib.request.urlretrieve(url, imagenet_classes_path)
                logger.info(f"Zapisano imagenet_classes.txt w {imagenet_classes_path}")
            else:
                logger.info(f"Używanie istniejącego imagenet_classes.txt z {imagenet_classes_path}")
            
            with open(imagenet_classes_path, "r") as f:
                self.imagenet_classes = [line.strip() for line in f.readlines()]
        except Exception as e:
            logger.warning(f"Nie udało się pobrać klas ImageNet: {e}")
            # Fallback - podstawowe klasy
            self.imagenet_classes = []
            logger.info("Używanie uproszczonej klasyfikacji")
    
    def classify(self, image_bytes: bytes, top_k: int = 5) -> Dict[str, any]:
        """
        Klasyfikuje obraz i zwraca bezpośrednie wyniki ImageNet.
        
        Args:
            image_bytes: Obraz w formacie bytes
            top_k: Liczba top predykcji do zwrócenia (domyślnie 5)
            
        Returns:
            Dict z top-k klasami ImageNet i ich prawdopodobieństwami
        """
        try:
            # Otwórz obraz
            image = Image.open(io.BytesIO(image_bytes))
            
            # Konwertuj na RGB jeśli potrzeba
            if image.mode != "RGB":
                image = image.convert("RGB")
            
            # Przetwórz obraz
            input_tensor = self.transform(image).unsqueeze(0).to(self.device)
            
            # Wykonaj predykcję
            with torch.no_grad():
                outputs = self.model(input_tensor)
                probabilities = torch.nn.functional.softmax(outputs[0], dim=0)
            
            # Pobierz top-k predykcji
            top_k_prob, top_k_indices = torch.topk(probabilities, min(top_k, len(self.imagenet_classes)))
            
            # Przygotuj listę wyników
            predictions = []
            for prob, idx in zip(top_k_prob, top_k_indices):
                if idx < len(self.imagenet_classes):
                    imagenet_class = self.imagenet_classes[idx]
                    predictions.append({
                        "class": imagenet_class,
                        "confidence": round(prob.item(), 4)
                    })
            
            # Najlepsza predykcja
            top_prediction = predictions[0] if predictions else None
            
            return {
                "category": top_prediction["class"] if top_prediction else None,
                "confidence": top_prediction["confidence"] if top_prediction else 0.0,
                "predictions": predictions
            }
            
        except Exception as e:
            logger.error(f"Błąd podczas klasyfikacji: {e}", exc_info=True)
            raise ValueError(f"Nie udało się sklasyfikować obrazu: {str(e)}")


# Globalna instancja klasyfikatora (lazy loading)
_classifier_instance = None


def get_classifier() -> ImageClassifier:
    """Zwraca singleton instancji klasyfikatora."""
    global _classifier_instance
    if _classifier_instance is None:
        logger.info("Inicjalizacja klasyfikatora obrazów...")
        _classifier_instance = ImageClassifier()
        logger.info("Klasyfikator gotowy")
    return _classifier_instance
