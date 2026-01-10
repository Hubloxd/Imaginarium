import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { AlbumService, Album } from '../../services/album.service';
import { GroupsComponent } from '../groups/groups.component';

type TabType = 'photos' | 'albums' | 'groups';

interface AlbumGroup {
  year: number;
  albums: Album[];
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, GroupsComponent],
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

  constructor(
    public authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private albumService: AlbumService
  ) {}

  ngOnInit(): void {
    // Sprawdź query param 'tab' i ustaw aktywną zakładkę
    this.route.queryParams.subscribe(params => {
      if (params['tab'] === 'albums') {
        this.activeTab.set('albums');
        this.loadAlbums();
      }
    });
  }

  setActiveTab(tab: TabType): void {
    this.activeTab.set(tab);
    if (tab === 'albums') {
      this.loadAlbums();
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
}