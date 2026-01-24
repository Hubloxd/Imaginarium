import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environment/environment';

export interface Album {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
  coverMediaId?: string;
  coverThumbnailUrl?: string;
  mediaCount: number;
  userId?: string;
}

export interface Tag {
  id: string;
  name: string;
  category?: string;
  confidence: number;
  source?: string;
}

export interface Media {
  id: string;
  fileName: string;
  filePath: string;
  mediaUrl: string;
  fileSize: number;
  mediaType: string;
  mimeType: string;
  uploadedAt: string;
  width?: number;
  height?: number;
  duration?: number;
  thumbnailPath?: string;
  thumbnailUrl?: string;
  tags?: Tag[];
}

export interface AlbumDetail extends Album {
  media: Media[];
}

@Injectable({
  providedIn: 'root'
})
export class AlbumService {
  private readonly apiUrl = `${environment.apiUrl}/api/albums`;

  constructor(private http: HttpClient) {}

  getAlbums(): Observable<Album[]> {
    return this.http.get<Album[]>(this.apiUrl);
  }

  getAlbumById(id: string): Observable<AlbumDetail> {
    return this.http.get<AlbumDetail>(`${this.apiUrl}/${id}`);
  }

  createAlbum(name: string, files: File[]): Observable<Album> {
    const formData = new FormData();
    formData.append('name', name);
    
    files.forEach(file => {
      formData.append('files', file);
    });

    return this.http.post<Album>(this.apiUrl, formData);
  }

  deleteAlbum(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  addFilesToAlbum(albumId: string, files: File[]): Observable<AlbumDetail> {
    const formData = new FormData();
    
    files.forEach(file => {
      formData.append('files', file);
    });

    return this.http.post<AlbumDetail>(`${this.apiUrl}/${albumId}/media`, formData);
  }
}
