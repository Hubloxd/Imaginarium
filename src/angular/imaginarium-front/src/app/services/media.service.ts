import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';

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

@Injectable({
  providedIn: 'root'
})
export class MediaService {
  private readonly apiUrl = `${environment.apiUrl}/api/media`;

  constructor(private http: HttpClient) {}

  getMedia(): Observable<Media[]> {
    return this.http.get<Media[]>(this.apiUrl);
  }

  getUserMedia(): Observable<Media[]> {
    return this.http.get<Media[]>(this.apiUrl);
  }

  getMediaById(id: string): Observable<Media> {
    return this.http.get<Media>(`${this.apiUrl}/${id}`);
  }

  updateMedia(id: string, file: File): Observable<Media> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.put<Media>(`${this.apiUrl}/${id}`, formData);
  }

  deleteMedia(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  searchMedia(params: {
    query?: string;
    tag?: string;
    date?: string; // ISO date string
    albumId?: string;
  }): Observable<Media[]> {
    const queryParams: any = {};
    if (params.query) queryParams.query = params.query;
    if (params.tag) queryParams.tag = params.tag;
    if (params.date) queryParams.date = params.date;
    if (params.albumId) queryParams.albumId = params.albumId;

    return this.http.get<Media[]>(`${this.apiUrl}/search`, { params: queryParams });
  }
}
