import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ShareService } from '../../services/share.service';
import { AlbumDetail, Media } from '../../services/album.service';
import { MediaViewerComponent, MediaItem } from '../media-viewer/media-viewer.component';

@Component({
  selector: 'app-public-share',
  standalone: true,
  imports: [CommonModule, MediaViewerComponent],
  templateUrl: './public-share.component.html',
  styleUrl: './public-share.component.css'
})
export class PublicShareComponent implements OnInit {
  token = signal<string | null>(null);
  isLoading = signal<boolean>(false);
  error = signal<string | null>(null);
  
  // Content data
  album = signal<AlbumDetail | null>(null);
  media = signal<Media | null>(null);
  
  // Media viewer
  showViewer = signal<boolean>(false);
  viewerMedia = signal<MediaItem[]>([]);
  viewerIndex = signal<number>(0);

  constructor(
    private route: ActivatedRoute,
    private shareService: ShareService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const token = params.get('token');
      if (token) {
        this.token.set(token);
        this.loadShareContent(token);
      } else {
        this.error.set('Brak tokenu udostępnienia');
      }
    });
  }

  loadShareContent(token: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.shareService.getShareContentByToken(token).subscribe({
      next: (content) => {
        // Sprawdź czy to album czy media
        // Album ma pole 'media' jako tablicę
        if (content.media && Array.isArray(content.media)) {
          // To jest album
          this.album.set(content);
        } else if (content.id && content.mediaUrl) {
          // To jest pojedyncze media (ma mediaUrl, ale nie ma tablicy media)
          this.media.set(content);
        } else {
          this.error.set('Nieprawidłowy format danych');
        }
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas ładowania udostępnionej treści:', error);
        if (error.status === 404) {
          this.error.set('Udostępnienie nie istnieje lub wygasło');
        } else if (error.status === 401 || error.status === 403) {
          this.error.set('Brak dostępu do tego udostępnienia');
        } else {
          this.error.set('Nie udało się załadować udostępnionej treści');
        }
        this.isLoading.set(false);
      }
    });
  }

  openMediaViewer(media: Media, allMedias: Media[]): void {
    const mediaItems: MediaItem[] = allMedias.map(m => ({
      id: m.id,
      mediaUrl: m.mediaUrl,
      thumbnailUrl: m.thumbnailUrl,
      mediaType: m.mediaType,
      mimeType: m.mimeType,
      fileName: m.fileName,
      uploadedAt: m.uploadedAt,
      tags: m.tags
    }));

    const index = allMedias.findIndex(m => m.id === media.id);
    this.viewerIndex.set(index >= 0 ? index : 0);
    this.viewerMedia.set(mediaItems);
    this.showViewer.set(true);
  }

  closeViewer(): void {
    this.showViewer.set(false);
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('pl-PL', { 
      day: 'numeric', 
      month: 'long',
      year: 'numeric'
    });
  }

  formatTime(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleTimeString('pl-PL', { 
      hour: '2-digit', 
      minute: '2-digit' 
    });
  }

  getMediaTags(): any[] {
    const m = this.media();
    return m?.tags || [];
  }
}
