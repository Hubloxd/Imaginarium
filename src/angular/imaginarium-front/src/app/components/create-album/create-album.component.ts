import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environment/environment';

@Component({
  selector: 'app-create-album',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-album.component.html',
  styleUrl: './create-album.component.css'
})
export class CreateAlbumComponent {
  albumTitle = signal<string>('Nowy album');
  files = signal<File[]>([]);
  isDragging = signal<boolean>(false);
  isUploading = signal<boolean>(false);
  uploadProgress = signal<number>(0);

  constructor(
    private router: Router,
    private http: HttpClient,
    public authService: AuthService
  ) {}

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);

    const droppedFiles = event.dataTransfer?.files;
    if (droppedFiles && droppedFiles.length > 0) {
      const files = Array.from(droppedFiles);
      this.handleFiles(files);
      // Automatycznie utwórz album po upuszczeniu plików
      if (this.files().length > 0) {
        this.createAlbum();
      }
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const files = Array.from(input.files);
      this.handleFiles(files);
      // Automatycznie utwórz album po wybraniu plików
      if (this.files().length > 0) {
        this.createAlbum();
      }
    }
  }

  private handleFiles(files: File[]): void {
    // Filtruj tylko obrazy i filmy
    const validFiles = files.filter(file => {
      const type = file.type.toLowerCase();
      return type.startsWith('image/') || type.startsWith('video/');
    });

    if (validFiles.length === 0) {
      alert('Proszę wybrać tylko pliki graficzne lub wideo.');
      return;
    }

    this.files.update(current => [...current, ...validFiles]);
  }

  removeFile(index: number): void {
    this.files.update(files => files.filter((_, i) => i !== index));
  }

  getFileIcon(file: File): string {
    if (file.type.startsWith('image/')) {
      return 'image';
    } else if (file.type.startsWith('video/')) {
      return 'video';
    }
    return 'file';
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }

  async createAlbum(): Promise<void> {
    if (this.files().length === 0) {
      alert('Proszę dodać przynajmniej jeden plik.');
      return;
    }

    if (this.isUploading()) {
      return; // Zapobiegaj wielokrotnemu wywołaniu
    }

    this.isUploading.set(true);
    this.uploadProgress.set(10);

    try {
      const formData = new FormData();
      formData.append('name', this.albumTitle());
      
      // Dodaj wszystkie pliki
      this.files().forEach((file) => {
        formData.append('files', file);
      });

      this.uploadProgress.set(30);

      // TODO: Zastąp tymczasowym endpointem - później będzie /api/albums
      const token = this.authService.getAccessToken();
      if (!token) {
        throw new Error('Brak tokenu autoryzacji');
      }

      const response = await fetch(`${environment.apiUrl}/api/albums`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        },
        body: formData
      });

      this.uploadProgress.set(80);

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Błąd podczas tworzenia albumu');
      }

      this.uploadProgress.set(100);
      const result = await response.json();
      
      // Przekieruj do strony głównej z zakładką albumów
      setTimeout(() => {
        this.router.navigate(['/'], { queryParams: { tab: 'albums' } });
      }, 500);
    } catch (error) {
      console.error('Błąd podczas tworzenia albumu:', error);
      alert('Wystąpił błąd podczas tworzenia albumu. Spróbuj ponownie.');
      this.isUploading.set(false);
      this.uploadProgress.set(0);
    }
  }

  cancel(): void {
    this.router.navigate(['/'], { queryParams: { tab: 'albums' } });
  }
}
