import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';

export enum Permission {
  View = 1,
  Download = 2,
  Edit = 3
}

export interface Share {
  id: string;
  mediaId?: string;
  albumId?: string;
  sharedByUserId: string;
  sharedByUsername: string;
  sharedWithUserId?: string;
  sharedWithUsername?: string;
  sharedWithGroupId?: string;
  sharedWithGroupName?: string;
  shareToken: string;
  isPublic: boolean;
  expiresAt?: string;
  createdAt: string;
  permissionLevel: Permission;
}

export interface CreateShareDto {
  mediaId?: string;
  albumId?: string;
  sharedWithUserId?: string;
  sharedWithUserEmail?: string;
  sharedWithGroupId?: string;
  isPublic?: boolean;
  expiresAt?: string;
  permissionLevel?: Permission;
}

@Injectable({
  providedIn: 'root'
})
export class ShareService {
  private readonly apiUrl = `${environment.apiUrl}/api/shares`;

  constructor(private http: HttpClient) {}

  createShare(dto: CreateShareDto): Observable<Share> {
    return this.http.post<Share>(this.apiUrl, dto);
  }

  getShareByToken(token: string): Observable<Share> {
    return this.http.get<Share>(`${this.apiUrl}/token/${token}`);
  }

  validateAccess(token: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}/validate/${token}`);
  }

  revokeShare(shareId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${shareId}`);
  }

  shareWithGroup(dto: CreateShareDto): Observable<Share> {
    return this.http.post<Share>(`${this.apiUrl}/group`, dto);
  }

  getGroupShares(groupId: string): Observable<Share[]> {
    return this.http.get<Share[]>(`${this.apiUrl}/group/${groupId}`);
  }

  getSharesForMe(): Observable<Share[]> {
    return this.http.get<Share[]>(`${this.apiUrl}/for-me`);
  }

  getSharesByMe(): Observable<Share[]> {
    return this.http.get<Share[]>(`${this.apiUrl}/by-me`);
  }
}
