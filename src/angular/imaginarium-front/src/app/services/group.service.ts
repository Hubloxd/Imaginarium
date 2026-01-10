import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';

export enum GroupRole {
  Admin = 1,
  Member = 2,
  Viewer = 3
}

export interface Group {
  id: string;
  name: string;
  description?: string;
  createdByUserId: string;
  createdByUsername: string;
  createdAt: string;
  updatedAt: string;
  isPrivate: boolean;
  memberCount: number;
  userRole?: GroupRole;
}

export interface GroupMember {
  id: string;
  groupId: string;
  groupName: string;
  userId: string;
  username: string;
  email: string;
  role: GroupRole;
  joinedAt: string;
  invitedByUserId?: string;
  invitedByUsername?: string;
}

export interface CreateGroupDto {
  name: string;
  description?: string;
  isPrivate: boolean;
}

export interface InviteUserDto {
  email: string;
  role: GroupRole;
}

@Injectable({
  providedIn: 'root'
})
export class GroupService {
  private readonly apiUrl = `${environment.apiUrl}/api/groups`;

  constructor(private http: HttpClient) {}

  getGroups(): Observable<Group[]> {
    return this.http.get<Group[]>(this.apiUrl);
  }

  getGroupById(id: string): Observable<Group> {
    return this.http.get<Group>(`${this.apiUrl}/${id}`);
  }

  createGroup(dto: CreateGroupDto): Observable<Group> {
    return this.http.post<Group>(this.apiUrl, dto);
  }

  deleteGroup(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getPendingInvitations(): Observable<GroupMember[]> {
    return this.http.get<GroupMember[]>(`${this.apiUrl}/invitations`);
  }

  inviteUser(groupId: string, dto: InviteUserDto): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${groupId}/invite`, dto);
  }

  acceptInvitation(groupId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${groupId}/accept`, {});
  }

  rejectInvitation(groupId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${groupId}/reject`, {});
  }

  getGroupMembers(groupId: string): Observable<GroupMember[]> {
    return this.http.get<GroupMember[]>(`${this.apiUrl}/${groupId}/members`);
  }

  removeMember(groupId: string, memberId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${groupId}/members/${memberId}`);
  }

  updateMemberRole(groupId: string, memberId: string, role: GroupRole): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${groupId}/members/${memberId}/role`, { role });
  }

  leaveGroup(groupId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${groupId}/leave`, {});
  }
}
