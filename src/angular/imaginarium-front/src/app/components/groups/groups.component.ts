import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { GroupService, Group, GroupMember, CreateGroupDto, InviteUserDto, GroupRole } from '../../services/group.service';

@Component({
  selector: 'app-groups',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './groups.component.html',
  styleUrl: './groups.component.css'
})
export class GroupsComponent implements OnInit {
  groups = signal<Group[]>([]);
  invitations = signal<GroupMember[]>([]);
  isLoadingGroups = signal<boolean>(false);
  isLoadingInvitations = signal<boolean>(false);
  groupsError = signal<string | null>(null);
  invitationsError = signal<string | null>(null);
  
  showCreateModal = signal<boolean>(false);
  newGroupName = signal<string>('');
  newGroupDescription = signal<string>('');
  newGroupIsPrivate = signal<boolean>(false);

  constructor(
    private groupService: GroupService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadGroups();
    this.loadInvitations();
  }

  loadGroups(): void {
    this.isLoadingGroups.set(true);
    this.groupsError.set(null);

    this.groupService.getGroups().subscribe({
      next: (groups) => {
        this.groups.set(groups);
        this.isLoadingGroups.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas pobierania grup:', error);
        this.groupsError.set('Nie udało się załadować grup.');
        this.isLoadingGroups.set(false);
      }
    });
  }

  loadInvitations(): void {
    this.isLoadingInvitations.set(true);
    this.invitationsError.set(null);

    this.groupService.getPendingInvitations().subscribe({
      next: (invitations) => {
        this.invitations.set(invitations);
        this.isLoadingInvitations.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas pobierania zaproszeń:', error);
        this.invitationsError.set('Nie udało się załadować zaproszeń.');
        this.isLoadingInvitations.set(false);
      }
    });
  }

  openCreateModal(): void {
    this.showCreateModal.set(true);
    this.newGroupName.set('');
    this.newGroupDescription.set('');
    this.newGroupIsPrivate.set(false);
  }

  closeCreateModal(): void {
    this.showCreateModal.set(false);
  }

  createGroup(): void {
    if (!this.newGroupName().trim()) {
      return;
    }

    const dto: CreateGroupDto = {
      name: this.newGroupName().trim(),
      description: this.newGroupDescription().trim() || undefined,
      isPrivate: this.newGroupIsPrivate()
    };

    this.groupService.createGroup(dto).subscribe({
      next: (group) => {
        this.groups.set([group, ...this.groups()]);
        this.closeCreateModal();
      },
      error: (error) => {
        console.error('Błąd podczas tworzenia grupy:', error);
        alert('Nie udało się utworzyć grupy.');
      }
    });
  }

  acceptInvitation(invitation: GroupMember): void {
    this.groupService.acceptInvitation(invitation.groupId).subscribe({
      next: () => {
        this.invitations.set(this.invitations().filter(inv => inv.id !== invitation.id));
        this.loadGroups(); // Odśwież listę grup
      },
      error: (error) => {
        console.error('Błąd podczas akceptowania zaproszenia:', error);
        alert('Nie udało się zaakceptować zaproszenia.');
      }
    });
  }

  rejectInvitation(invitation: GroupMember): void {
    this.groupService.rejectInvitation(invitation.groupId).subscribe({
      next: () => {
        this.invitations.set(this.invitations().filter(inv => inv.id !== invitation.id));
      },
      error: (error) => {
        console.error('Błąd podczas odrzucania zaproszenia:', error);
        alert('Nie udało się odrzucić zaproszenia.');
      }
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('pl-PL', { 
      day: 'numeric', 
      month: 'long',
      year: 'numeric'
    });
  }

  getRoleName(role?: GroupRole): string {
    if (!role) return '';
    switch (role) {
      case GroupRole.Admin:
        return 'Administrator';
      case GroupRole.Member:
        return 'Członek';
      case GroupRole.Viewer:
        return 'Przeglądający';
      default:
        return '';
    }
  }

  openGroup(groupId: string): void {
    this.router.navigate(['/groups', groupId]);
  }
}
