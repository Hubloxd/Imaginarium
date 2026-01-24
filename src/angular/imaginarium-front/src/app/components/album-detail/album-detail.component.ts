import { Component, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { interval, Subscription } from 'rxjs';
import { AlbumService, Album, AlbumDetail, Media } from '../../services/album.service';
import { MediaService } from '../../services/media.service';
import { AuthService } from '../../services/auth.service';
import { MediaViewerComponent, MediaItem } from '../media-viewer/media-viewer.component';
import { ContextMenuComponent, ContextMenuItem } from '../context-menu/context-menu.component';
import { ShareModalComponent } from '../share-modal/share-modal.component';

interface MediaGroup {
  date: string;
  medias: Media[];
}

@Component({
  selector: 'app-album-detail',
  standalone: true,
  imports: [CommonModule, MediaViewerComponent, ContextMenuComponent, ShareModalComponent],
  templateUrl: './album-detail.component.html',
  styleUrl: './album-detail.component.css'
})
export class AlbumDetailComponent implements OnInit, OnDestroy {
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
  mediaSearchQuery = signal<string>('');
  mediaSearchTag = signal<string>('');
  mediaSearchDate = signal<string>('');

  // Thumbnail retry tracking
  private thumbnailRetryMap = new Map<string, number>(); // mediaId -> retry count
  private thumbnailRetrySubscription?: Subscription;

  // Context menu
  contextMenuVisible = signal<boolean>(false);
  contextMenuX = signal<number>(0);
  contextMenuY = signal<number>(0);
  contextMenuItems = signal<ContextMenuItem[]>([]);
  selectedMedia: Media | null = null;

  // Share modal
  showShareModal = signal<boolean>(false);
  shareMediaId = signal<string | undefined>(undefined);
  shareAlbumId = signal<string | undefined>(undefined);

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
    private mediaService: MediaService,
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

    // Uruchom polling dla miniaturek co 3 sekundy
    this.thumbnailRetrySubscription = interval(3000).subscribe(() => {
      this.retryThumbnails();
    });
  }

  ngOnDestroy(): void {
    if (this.thumbnailRetrySubscription) {
      this.thumbnailRetrySubscription.unsubscribe();
    }
  }

  loadAlbum(id: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.albumService.getAlbumById(id).subscribe({
      next: (album) => {
        this.album.set(album);
        // Filtruj media lokalnie jeśli są kryteria wyszukiwania
        let filteredMedias = album.media || [];
        
        if (this.mediaSearchQuery().trim()) {
          const query = this.mediaSearchQuery().toLowerCase().trim();
          filteredMedias = filteredMedias.filter(m => 
            m.fileName.toLowerCase().includes(query) ||
            (m.tags && m.tags.some(t => t.name.toLowerCase().includes(query)))
          );
        }
        
        if (this.mediaSearchTag().trim()) {
          const tag = this.mediaSearchTag().toLowerCase().trim();
          filteredMedias = filteredMedias.filter(m => 
            m.tags && m.tags.some(t => t.name.toLowerCase().includes(tag))
          );
        }
        
        if (this.mediaSearchDate().trim()) {
          // Parsuj datę z inputa (format YYYY-MM-DD) i utwórz datę w UTC
          const dateStr = this.mediaSearchDate().trim();
          const [year, month, day] = dateStr.split('-').map(Number);
          const searchDateStart = new Date(Date.UTC(year, month - 1, day, 0, 0, 0, 0));
          const searchDateEnd = new Date(searchDateStart);
          searchDateEnd.setUTCDate(searchDateEnd.getUTCDate() + 1);
          
          filteredMedias = filteredMedias.filter(m => {
            const uploadedDate = new Date(m.uploadedAt);
            // Porównaj tylko daty (bez czasu), używając UTC
            const uploadedDateUTC = new Date(Date.UTC(
              uploadedDate.getUTCFullYear(),
              uploadedDate.getUTCMonth(),
              uploadedDate.getUTCDate()
            ));
            const searchDateStartUTC = new Date(Date.UTC(
              searchDateStart.getUTCFullYear(),
              searchDateStart.getUTCMonth(),
              searchDateStart.getUTCDate()
            ));
            const searchDateEndUTC = new Date(Date.UTC(
              searchDateEnd.getUTCFullYear(),
              searchDateEnd.getUTCMonth(),
              searchDateEnd.getUTCDate()
            ));
            return uploadedDateUTC >= searchDateStartUTC && uploadedDateUTC < searchDateEndUTC;
          });
        }
        
        this.medias.set(filteredMedias);
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

  onMediaSearchChange(): void {
    // Przeładuj album z filtrowaniem
    const albumId = this.albumId();
    if (albumId) {
      this.loadAlbum(albumId);
    }
  }

  clearMediaSearch(): void {
    this.mediaSearchQuery.set('');
    this.mediaSearchTag.set('');
    this.mediaSearchDate.set('');
    const albumId = this.albumId();
    if (albumId) {
      this.loadAlbum(albumId);
    }
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

  onMediaContextMenu(event: MouseEvent, media: Media): void {
    event.preventDefault();
    event.stopPropagation();
    
    this.selectedMedia = media;
    this.contextMenuX.set(event.clientX);
    this.contextMenuY.set(event.clientY);
    
    this.contextMenuItems.set([
      {
        label: 'Udostępnij',
        icon: 'M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z',
        action: () => this.openShareModal(media.id, undefined)
      },
      {
        label: 'Usuń',
        icon: 'M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16',
        action: () => this.deleteMedia(media.id)
      }
    ]);
    
    this.contextMenuVisible.set(true);
  }

  deleteMedia(mediaId: string): void {
    if (!confirm('Czy na pewno chcesz usunąć to zdjęcie? Ta operacja jest nieodwracalna.')) {
      return;
    }

    this.mediaService.deleteMedia(mediaId).subscribe({
      next: () => {
        // Przeładuj album po usunięciu
        const albumId = this.albumId();
        if (albumId) {
          this.loadAlbum(albumId);
        }
        this.closeContextMenu();
      },
      error: (error) => {
        console.error('Błąd podczas usuwania media:', error);
        alert('Nie udało się usunąć zdjęcia.');
      }
    });
  }

  onAlbumContextMenu(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    
    this.contextMenuX.set(event.clientX);
    this.contextMenuY.set(event.clientY);
    
    const album = this.album();
    if (!album) return;
    
    const currentUser = this.authService.getUser();
    const isOwner = currentUser && album.userId && currentUser.id === album.userId;
    
    const menuItems: ContextMenuItem[] = [
      {
        label: 'Udostępnij album',
        icon: 'M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z',
        action: () => this.openShareModal(undefined, album.id)
      }
    ];
    
    // Dodaj opcję usuwania tylko dla właściciela
    if (isOwner) {
      menuItems.push({
        label: 'Usuń album',
        icon: 'M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16',
        action: () => this.deleteAlbum()
      });
    }
    
    this.contextMenuItems.set(menuItems);
    this.contextMenuVisible.set(true);
  }

  deleteAlbum(): void {
    const album = this.album();
    if (!album) return;
    
    const currentUser = this.authService.getUser();
    if (!currentUser || !album.userId || currentUser.id !== album.userId) {
      alert('Nie masz uprawnień do usunięcia tego albumu.');
      return;
    }
    
    if (!confirm(`Czy na pewno chcesz usunąć album "${album.name}"? Ta operacja jest nieodwracalna.`)) {
      return;
    }
    
    this.albumService.deleteAlbum(album.id).subscribe({
      next: () => {
        this.closeContextMenu();
        this.goBack();
      },
      error: (error) => {
        console.error('Błąd podczas usuwania albumu:', error);
        alert('Nie udało się usunąć albumu.');
      }
    });
  }

  closeContextMenu(): void {
    this.contextMenuVisible.set(false);
  }

  openShareModal(mediaId?: string, albumId?: string): void {
    this.shareMediaId.set(mediaId);
    this.shareAlbumId.set(albumId);
    this.showShareModal.set(true);
  }

  closeShareModal(): void {
    this.showShareModal.set(false);
    this.shareMediaId.set(undefined);
    this.shareAlbumId.set(undefined);
  }

  onShared(): void {
    // Można dodać powiadomienie o udostępnieniu
    this.closeShareModal();
  }

  onMediaUpdated(): void {
    // Przeładuj album po aktualizacji
    const albumId = this.albumId();
    if (albumId) {
      this.loadAlbum(albumId);
    }
  }

  onMediaDeleted(mediaId: string): void {
    // Przeładuj album po usunięciu
    const albumId = this.albumId();
    if (albumId) {
      this.loadAlbum(albumId);
    }
    // Usuń z mapy retry
    this.thumbnailRetryMap.delete(mediaId);
  }

  onThumbnailLoadError(mediaId: string, event: Event): void {
    const img = event.target as HTMLImageElement;
    const retryCount = this.thumbnailRetryMap.get(mediaId) || 0;
    
    // Próbuj maksymalnie 20 razy (60 sekund)
    if (retryCount < 20) {
      this.thumbnailRetryMap.set(mediaId, retryCount + 1);
      
      // Spróbuj ponownie załadować z cache-busting
      setTimeout(() => {
        const media = this.medias().find(m => m.id === mediaId);
        if (media && media.thumbnailUrl) {
          // Dodaj timestamp do URL dla cache-busting
          const separator = media.thumbnailUrl.includes('?') ? '&' : '?';
          img.src = `${media.thumbnailUrl}${separator}v=${Date.now()}`;
        }
      }, 100);
    } else {
      // Po 20 próbach, pokaż placeholder
      img.style.display = 'none';
      const fallback = img.nextElementSibling as HTMLElement;
      if (fallback) {
        fallback.classList.remove('hidden');
      }
      this.thumbnailRetryMap.delete(mediaId);
    }
  }

  onThumbnailLoadSuccess(mediaId: string): void {
    // Usuń z mapy retry po udanym załadowaniu
    this.thumbnailRetryMap.delete(mediaId);
  }

  private retryThumbnails(): void {
    // Dla mediów bez miniaturek, które są świeżo dodane, spróbuj przeładować album
    const mediasWithoutThumbnails = this.medias().filter(m => 
      !m.thumbnailUrl && 
      this.thumbnailRetryMap.get(m.id) === undefined &&
      new Date(m.uploadedAt).getTime() > Date.now() - 60000 // Tylko media z ostatniej minuty
    );

    if (mediasWithoutThumbnails.length > 0) {
      // Przeładuj album, aby sprawdzić czy miniaturki są już dostępne
      const albumId = this.albumId();
      if (albumId) {
        this.loadAlbum(albumId);
      }
    }
  }
}
