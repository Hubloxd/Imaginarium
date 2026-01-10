import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';

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

  getMediaById(id: string): Observable<Media> {
    return this.http.get<Media>(`${this.apiUrl}/${id}`);
  }
}
