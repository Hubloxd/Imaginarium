import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { AlbumService, Album, AlbumDetail, Media } from '../../services/album.service';
import { AuthService } from '../../services/auth.service';
import { MediaViewerComponent, MediaItem } from '../media-viewer/media-viewer.component';

interface MediaGroup {
  date: string;
  medias: Media[];
}

@Component({
  selector: 'app-album-detail',
  standalone: true,
  imports: [CommonModule, MediaViewerComponent],
  templateUrl: './album-detail.component.html',
  styleUrl: './album-detail.component.css'
})
export class AlbumDetailComponent implements OnInit {
  album = signal<AlbumDetail | null>(null);
  medias = signal<Media[]>([]);
  isLoading = signal<boolean>(false);
  error = signal<string | null>(null);
  albumId = signal<string | null>(null);
  showViewer = signal<boolean>(false);
  viewerMedia = signal<MediaItem[]>([]);
  viewerIndex = signal<number>(0);
  isDragging = signal<boolean>(false);
  isUploading = signal<boolean>(false);

  groupedMedias = computed(() => {
    const allMedias = this.medias();
    
    // Sortuj od najnowszych do najstarszych
    const sorted = [...allMedias].sort((a, b) => 
      new Date(b.uploadedAt).getTime() - new Date(a.uploadedAt).getTime()
    );

    // Grupuj według dnia
    const groups = new Map<string, Media[]>();
    
    sorted.forEach(media => {
      const date = new Date(media.uploadedAt);
      const dateKey = date.toLocaleDateString('pl-PL', { 
        year: 'numeric', 
        month: 'long', 
        day: 'numeric' 
      });
      
      if (!groups.has(dateKey)) {
        groups.set(dateKey, []);
      }
      groups.get(dateKey)!.push(media);
    });

    // Konwertuj na tablicę i sortuj daty od najnowszych
    return Array.from(groups.entries())
      .map(([date, medias]) => ({ date, medias }))
      .sort((a, b) => 
        new Date(b.medias[0].uploadedAt).getTime() - new Date(a.medias[0].uploadedAt).getTime()
      );
  });

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private albumService: AlbumService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.albumId.set(id);
        this.loadAlbum(id);
      }
    });
  }

  loadAlbum(id: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.albumService.getAlbumById(id).subscribe({
      next: (album) => {
        this.album.set(album);
        this.medias.set(album.media || []);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas pobierania albumu:', error);
        this.error.set('Nie udało się załadować albumu.');
        this.isLoading.set(false);
      }
    });
  }

  loadMedias(albumId: string): void {
    // Media są już załadowane w loadAlbum
  }

  goBack(): void {
    this.router.navigate(['/'], { queryParams: { tab: 'albums' } });
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

  openMediaViewer(media: Media, allMedias: Media[]): void {
    const mediaItems: MediaItem[] = allMedias.map(m => ({
      id: m.id,
      mediaUrl: m.mediaUrl,
      thumbnailUrl: m.thumbnailUrl,
      mediaType: m.mediaType,
      mimeType: m.mimeType,
      fileName: m.fileName,
      uploadedAt: m.uploadedAt
    }));

    const index = allMedias.findIndex(m => m.id === media.id);
    this.viewerIndex.set(index >= 0 ? index : 0);
    this.viewerMedia.set(mediaItems);
    this.showViewer.set(true);
  }

  closeViewer(): void {
    this.showViewer.set(false);
  }

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
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const files = Array.from(input.files);
      this.handleFiles(files);
    }
  }

  private handleFiles(files: File[]): void {
    const albumId = this.albumId();
    if (!albumId) return;

    // Filtruj tylko obrazy i filmy
    const validFiles = files.filter(file => {
      const type = file.type.toLowerCase();
      return type.startsWith('image/') || type.startsWith('video/');
    });

    if (validFiles.length === 0) {
      alert('Proszę wybrać tylko pliki graficzne lub wideo.');
      return;
    }

    this.uploadFiles(validFiles);
  }

  uploadFiles(files: File[]): void {
    const albumId = this.albumId();
    if (!albumId) return;

    this.isUploading.set(true);
    this.error.set(null);

    this.albumService.addFilesToAlbum(albumId, files).subscribe({
      next: (updatedAlbum) => {
        this.album.set(updatedAlbum);
        this.medias.set(updatedAlbum.media || []);
        this.isUploading.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas dodawania plików:', error);
        this.error.set('Nie udało się dodać plików do albumu.');
        this.isUploading.set(false);
      }
    });
  }

  triggerFileInput(): void {
    const input = document.getElementById('file-input') as HTMLInputElement;
    input?.click();
  }
}
