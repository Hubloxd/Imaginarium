import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MediaService } from '../../services/media.service';

@Component({
  selector: 'app-media-edit-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './media-edit-modal.component.html',
  styleUrl: './media-edit-modal.component.css'
})
export class MediaEditModalComponent {
  @Input() visible = signal<boolean>(false);
  @Input() mediaId: string | null = null;
  @Input() mediaFileName: string = '';
  @Output() close = new EventEmitter<void>();
  @Output() updated = new EventEmitter<void>();
  @Output() deleted = new EventEmitter<void>();

  isDragging = signal<boolean>(false);
  isUploading = signal<boolean>(false);
  isDeleting = signal<boolean>(false);
  selectedFile: File | null = null;
  error = signal<string | null>(null);

  constructor(private mediaService: MediaService) {}

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
      const file = droppedFiles[0];
      this.handleFile(file);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.handleFile(file);
    }
  }

  handleFile(file: File): void {
    // Sprawdź czy to obraz lub wideo
    if (!file.type.startsWith('image/') && !file.type.startsWith('video/')) {
      this.error.set('Proszę wybrać tylko pliki graficzne lub wideo.');
      return;
    }

    this.selectedFile = file;
    this.error.set(null);
  }

  uploadFile(): void {
    if (!this.selectedFile || !this.mediaId) {
      return;
    }

    this.isUploading.set(true);
    this.error.set(null);

    this.mediaService.updateMedia(this.mediaId, this.selectedFile).subscribe({
      next: () => {
        this.isUploading.set(false);
        this.selectedFile = null;
        this.updated.emit();
        this.closeModal();
      },
      error: (error) => {
        console.error('Błąd podczas aktualizacji media:', error);
        this.error.set('Nie udało się zaktualizować pliku.');
        this.isUploading.set(false);
      }
    });
  }

  deleteMedia(): void {
    if (!this.mediaId) {
      return;
    }

    if (!confirm('Czy na pewno chcesz usunąć to zdjęcie? Ta operacja jest nieodwracalna.')) {
      return;
    }

    this.isDeleting.set(true);
    this.error.set(null);

    this.mediaService.deleteMedia(this.mediaId).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.deleted.emit();
        this.closeModal();
      },
      error: (error) => {
        console.error('Błąd podczas usuwania media:', error);
        this.error.set('Nie udało się usunąć pliku.');
        this.isDeleting.set(false);
      }
    });
  }

  closeModal(): void {
    this.visible.set(false);
    this.selectedFile = null;
    this.error.set(null);
    this.isDragging.set(false);
    this.close.emit();
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }
}
