import { Component, signal, Input, Output, EventEmitter, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MediaEditModalComponent } from '../media-edit-modal/media-edit-modal.component';

export interface Tag {
  id: string;
  name: string;
  category?: string;
  confidence: number;
  source?: string;
}

export interface MediaItem {
  id: string;
  mediaUrl: string;
  thumbnailUrl?: string;
  mediaType: string;
  mimeType: string;
  fileName: string;
  uploadedAt: string;
  tags?: Tag[];
}

@Component({
  selector: 'app-media-viewer',
  standalone: true,
  imports: [CommonModule, MediaEditModalComponent],
  templateUrl: './media-viewer.component.html',
  styleUrl: './media-viewer.component.css'
})
export class MediaViewerComponent implements OnInit, OnDestroy {
  @Input() media: MediaItem[] = [];
  @Input() currentIndex: number = 0;
  @Output() close = new EventEmitter<void>();
  @Output() mediaUpdated = new EventEmitter<void>();
  @Output() mediaDeleted = new EventEmitter<string>();

  currentMedia = signal<MediaItem | null>(null);
  isLoading = signal<boolean>(false);
  error = signal<string | null>(null);
  showEditModal = signal<boolean>(false);

  get mediaCount(): number {
    return this.media.length;
  }

  ngOnInit(): void {
    this.updateCurrentMedia();
    document.body.style.overflow = 'hidden';
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  @HostListener('document:keydown', ['$event'])
  handleKeyboard(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.closeViewer();
    } else if (event.key === 'ArrowLeft') {
      this.previous();
    } else if (event.key === 'ArrowRight') {
      this.next();
    }
  }

  updateCurrentMedia(): void {
    if (this.media && this.media.length > 0 && this.currentIndex >= 0 && this.currentIndex < this.media.length) {
      this.currentMedia.set(this.media[this.currentIndex]);
    }
  }

  next(): void {
    if (this.currentIndex < this.media.length - 1) {
      this.currentIndex++;
      this.updateCurrentMedia();
    }
  }

  previous(): void {
    if (this.currentIndex > 0) {
      this.currentIndex--;
      this.updateCurrentMedia();
    }
  }

  closeViewer(): void {
    this.close.emit();
  }

  onImageLoad(): void {
    this.isLoading.set(false);
  }

  onImageError(): void {
    this.isLoading.set(false);
    this.error.set('Nie udało się załadować obrazu.');
  }

  onVideoLoad(): void {
    this.isLoading.set(false);
  }

  onVideoError(): void {
    this.isLoading.set(false);
    this.error.set('Nie udało się załadować wideo.');
  }

  canGoNext(): boolean {
    return this.currentIndex < this.media.length - 1;
  }

  canGoPrevious(): boolean {
    return this.currentIndex > 0;
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('pl-PL', { 
      day: 'numeric', 
      month: 'long',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  openEditModal(): void {
    this.showEditModal.set(true);
  }

  closeEditModal(): void {
    this.showEditModal.set(false);
  }

  onMediaUpdated(): void {
    this.mediaUpdated.emit();
    this.closeEditModal();
    // Zamknij viewer, aby zresetować stan i przeładować media
    this.closeViewer();
  }

  onMediaDeleted(): void {
    const currentMedia = this.currentMedia();
    if (currentMedia) {
      this.mediaDeleted.emit(currentMedia.id);
    }
    this.closeEditModal();
    this.closeViewer();
  }
}
