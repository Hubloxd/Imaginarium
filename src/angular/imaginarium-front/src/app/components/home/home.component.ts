import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { AlbumService, Album } from '../../services/album.service';
import { MediaService, Media } from '../../services/media.service';
import { GroupsComponent } from '../groups/groups.component';
import { ContextMenuComponent, ContextMenuItem } from '../context-menu/context-menu.component';
import { ShareModalComponent } from '../share-modal/share-modal.component';
import { MediaViewerComponent, MediaItem } from '../media-viewer/media-viewer.component';

type TabType = 'photos' | 'albums' | 'groups';

interface AlbumGroup {
  year: number;
  albums: Album[];
}

interface MediaGroup {
  date: string;
  medias: Media[];
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, GroupsComponent, ContextMenuComponent, ShareModalComponent, MediaViewerComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  activeTab = signal<TabType>('photos');
  isSidebarOpen = signal(true);
  albumSearchQuery = signal<string>('');
  albums = signal<Album[]>([]);
  isLoadingAlbums = signal<boolean>(false);
  albumsError = signal<string | null>(null);

  // Photos
  medias = signal<Media[]>([]);
  isLoadingMedias = signal<boolean>(false);
  mediasError = signal<string | null>(null);
  showViewer = signal<boolean>(false);
  viewerMedia = signal<MediaItem[]>([]);
  viewerIndex = signal<number>(0);

  filteredAlbums = computed(() => {
    const query = this.albumSearchQuery().toLowerCase().trim();
    const allAlbums = this.albums();
    
    if (!query) {
      return allAlbums;
    }
    return allAlbums.filter(album => 
      album.name.toLowerCase().includes(query)
    );
  });

  groupedAlbums = computed(() => {
    const albums = this.filteredAlbums();
    
    // Sortuj od najnowszych do najstarszych
    const sorted = [...albums].sort((a, b) => 
      new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );

    // Grupuj według roku
    const groups = new Map<number, Album[]>();
    
    sorted.forEach(album => {
      const year = new Date(album.createdAt).getFullYear();
      if (!groups.has(year)) {
        groups.set(year, []);
      }
      groups.get(year)!.push(album);
    });

    // Konwertuj na tablicę i sortuj lata od najnowszych
    return Array.from(groups.entries())
      .map(([year, albums]) => ({ year, albums }))
      .sort((a, b) => b.year - a.year);
  });

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

    // Konwertuj na tablicę
    return Array.from(groups.entries())
      .map(([date, medias]) => ({ date, medias }));
  });

  constructor(
    public authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private albumService: AlbumService,
    private mediaService: MediaService
  ) {}

  ngOnInit(): void {
    // Sprawdź query param 'tab' i ustaw aktywną zakładkę
    this.route.queryParams.subscribe(params => {
      if (params['tab'] === 'albums') {
        this.activeTab.set('albums');
        this.loadAlbums();
      } else if (params['tab'] === 'groups') {
        this.activeTab.set('groups');
      } else {
        // Domyślnie zakładka "Zdjęcia"
        this.activeTab.set('photos');
        this.loadMedias();
      }
    });
  }

  setActiveTab(tab: TabType): void {
    this.activeTab.set(tab);
    if (tab === 'albums') {
      this.loadAlbums();
    } else if (tab === 'photos') {
      this.loadMedias();
    }
    // Groups component ładuje dane w ngOnInit
  }

  loadAlbums(): void {
    if (this.isLoadingAlbums()) {
      return;
    }

    this.isLoadingAlbums.set(true);
    this.albumsError.set(null);

    this.albumService.getAlbums().subscribe({
      next: (albums) => {
        this.albums.set(albums);
        this.isLoadingAlbums.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas pobierania albumów:', error);
        this.albumsError.set('Nie udało się załadować albumów.');
        this.isLoadingAlbums.set(false);
      }
    });
  }

  toggleSidebar(): void {
    this.isSidebarOpen.update(value => !value);
  }

  logout(): void {
    this.authService.logout();
  }

  onSearchChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.albumSearchQuery.set(target.value);
  }

  createAlbum(): void {
    this.router.navigate(['/albums/create']);
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('pl-PL', { day: 'numeric', month: 'long' });
  }

  openAlbum(albumId: string): void {
    this.router.navigate(['/albums', albumId]);
  }

  onThumbnailError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.style.display = 'none';
    const fallback = img.nextElementSibling as HTMLElement;
    if (fallback) {
      fallback.classList.remove('hidden');
    }
  }

  // Context menu
  contextMenuVisible = signal<boolean>(false);
  contextMenuX = signal<number>(0);
  contextMenuY = signal<number>(0);
  contextMenuItems = signal<ContextMenuItem[]>([]);
  selectedAlbum: Album | null = null;

  // Share modal
  showShareModal = signal<boolean>(false);
  shareMediaId = signal<string | undefined>(undefined);
  shareAlbumId = signal<string | undefined>(undefined);

  onAlbumContextMenu(event: MouseEvent, album: Album): void {
    event.preventDefault();
    event.stopPropagation();
    
    this.selectedAlbum = album;
    this.contextMenuX.set(event.clientX);
    this.contextMenuY.set(event.clientY);
    
    this.contextMenuItems.set([
      {
        label: 'Udostępnij album',
        icon: 'M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z',
        action: () => this.openShareModal(album.id)
      }
    ]);
    
    this.contextMenuVisible.set(true);
  }

  closeContextMenu(): void {
    this.contextMenuVisible.set(false);
  }

  openShareModal(albumId: string): void {
    this.shareAlbumId.set(albumId);
    this.shareMediaId.set(undefined);
    this.showShareModal.set(true);
  }

  closeShareModal(): void {
    this.showShareModal.set(false);
    this.shareAlbumId.set(undefined);
    this.shareMediaId.set(undefined);
  }

  onShared(): void {
    this.closeShareModal();
  }

  loadMedias(): void {
    if (this.isLoadingMedias()) {
      return;
    }

    this.isLoadingMedias.set(true);
    this.mediasError.set(null);

    this.mediaService.getMedia().subscribe({
      next: (medias) => {
        this.medias.set(medias);
        this.isLoadingMedias.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas pobierania mediów:', error);
        this.mediasError.set('Nie udało się załadować zdjęć.');
        this.isLoadingMedias.set(false);
      }
    });
  }

  openMediaViewer(media: Media, allMedias: Media[]): void {
    const mediaItems: MediaItem[] = allMedias.map(m => ({
      id: m.id,
      mediaUrl: m.mediaUrl,
      thumbnailUrl: m.thumbnailUrl || '',
      fileName: m.fileName,
      mediaType: m.mediaType,
      mimeType: m.mimeType,
      uploadedAt: m.uploadedAt
    }));

    const index = mediaItems.findIndex(m => m.id === media.id);
    this.viewerMedia.set(mediaItems);
    this.viewerIndex.set(index >= 0 ? index : 0);
    this.showViewer.set(true);
  }

  closeViewer(): void {
    this.showViewer.set(false);
  }

  formatTime(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('pl-PL', { 
      day: 'numeric', 
      month: 'long',
      year: 'numeric'
    });
  }

  onMediaContextMenu(event: MouseEvent, media: Media): void {
    event.preventDefault();
    event.stopPropagation();
    
    this.contextMenuX.set(event.clientX);
    this.contextMenuY.set(event.clientY);
    
    this.contextMenuItems.set([
      {
        label: 'Udostępnij',
        icon: 'M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z',
        action: () => this.openShareModalForMedia(media.id)
      }
    ]);
    
    this.contextMenuVisible.set(true);
  }

  openShareModalForMedia(mediaId: string): void {
    this.shareMediaId.set(mediaId);
    this.shareAlbumId.set(undefined);
    this.showShareModal.set(true);
  }
}