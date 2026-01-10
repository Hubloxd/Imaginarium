import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { GroupService, Group, GroupMember, InviteUserDto, GroupRole } from '../../services/group.service';

@Component({
  selector: 'app-group-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './group-detail.component.html',
  styleUrl: './group-detail.component.css'
})
export class GroupDetailComponent implements OnInit {
  // Export GroupRole for template use
  GroupRole = GroupRole;

  group = signal<Group | null>(null);
  members = signal<GroupMember[]>([]);
  isLoading = signal<boolean>(false);
  isLoadingMembers = signal<boolean>(false);
  error = signal<string | null>(null);
  membersError = signal<string | null>(null);

  showInviteModal = signal<boolean>(false);
  inviteEmail = signal<string>('');
  inviteRole = signal<GroupRole>(GroupRole.Member);
  isInviting = signal<boolean>(false);

  showEditRoleModal = signal<boolean>(false);
  editingMember = signal<GroupMember | null>(null);
  newRole = signal<GroupRole>(GroupRole.Member);
  isUpdatingRole = signal<boolean>(false);

  showDeleteMemberModal = signal<boolean>(false);
  memberToDelete = signal<GroupMember | null>(null);
  isDeletingMember = signal<boolean>(false);

  showDeleteGroupModal = signal<boolean>(false);
  isDeletingGroup = signal<boolean>(false);

  groupId: string = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private groupService: GroupService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.groupId = params['id'];
      this.loadGroup();
      this.loadMembers();
    });
  }

  loadGroup(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.groupService.getGroupById(this.groupId).subscribe({
      next: (group) => {
        this.group.set(group);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas pobierania grupy:', error);
        this.error.set('Nie udało się załadować grupy.');
        this.isLoading.set(false);
      }
    });
  }

  loadMembers(): void {
    this.isLoadingMembers.set(true);
    this.membersError.set(null);

    this.groupService.getGroupMembers(this.groupId).subscribe({
      next: (members) => {
        this.members.set(members);
        this.isLoadingMembers.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas pobierania członków:', error);
        this.membersError.set('Nie udało się załadować członków grupy.');
        this.isLoadingMembers.set(false);
      }
    });
  }

  openInviteModal(): void {
    this.showInviteModal.set(true);
    this.inviteEmail.set('');
    this.inviteRole.set(GroupRole.Member);
  }

  closeInviteModal(): void {
    this.showInviteModal.set(false);
    this.inviteEmail.set('');
    this.inviteRole.set(GroupRole.Member);
  }

  inviteUser(): void {
    if (!this.inviteEmail().trim()) {
      return;
    }

    this.isInviting.set(true);

    const inviteDto: InviteUserDto = {
      email: this.inviteEmail().trim(),
      role: this.inviteRole()
    };

    this.groupService.inviteUser(this.groupId, inviteDto).subscribe({
      next: () => {
        this.closeInviteModal();
        this.loadMembers(); // Odśwież listę członków
        this.loadGroup(); // Odśwież informacje o grupie (memberCount)
        this.isInviting.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas zapraszania użytkownika:', error);
        alert('Nie udało się zaprosić użytkownika. Sprawdź czy email jest poprawny i czy użytkownik nie jest już członkiem grupy.');
        this.isInviting.set(false);
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

  getRoleName(role: GroupRole): string {
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

  getRoleDescription(role: GroupRole): string {
    switch (role) {
      case GroupRole.Admin:
        return 'Pełne uprawnienia: zarządzanie grupą, członkami i rolami';
      case GroupRole.Member:
        return 'Może dodawać treści i zapraszać innych użytkowników';
      case GroupRole.Viewer:
        return 'Tylko przeglądanie treści grupy';
      default:
        return '';
    }
  }

  canManageMembers(): boolean {
    const group = this.group();
    if (!group || !group.userRole) return false;
    return group.userRole === GroupRole.Admin;
  }

  isCurrentUser(member: GroupMember): boolean {
    // Sprawdź czy to aktualny użytkownik - potrzebujemy AuthService
    // Na razie zwrócimy false, można dodać później
    return false;
  }

  openEditRoleModal(member: GroupMember): void {
    this.editingMember.set(member);
    this.newRole.set(member.role);
    this.showEditRoleModal.set(true);
  }

  closeEditRoleModal(): void {
    this.showEditRoleModal.set(false);
    this.editingMember.set(null);
  }

  updateMemberRole(): void {
    const member = this.editingMember();
    if (!member) return;

    this.isUpdatingRole.set(true);

    this.groupService.updateMemberRole(this.groupId, member.userId, this.newRole()).subscribe({
      next: () => {
        this.closeEditRoleModal();
        this.loadMembers();
        this.isUpdatingRole.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas aktualizacji roli:', error);
        alert('Nie udało się zaktualizować roli członka.');
        this.isUpdatingRole.set(false);
      }
    });
  }

  openDeleteMemberModal(member: GroupMember): void {
    this.memberToDelete.set(member);
    this.showDeleteMemberModal.set(true);
  }

  closeDeleteMemberModal(): void {
    this.showDeleteMemberModal.set(false);
    this.memberToDelete.set(null);
  }

  deleteMember(): void {
    const member = this.memberToDelete();
    if (!member) return;

    this.isDeletingMember.set(true);

    this.groupService.removeMember(this.groupId, member.userId).subscribe({
      next: () => {
        this.closeDeleteMemberModal();
        this.loadMembers();
        this.loadGroup(); // Odśwież memberCount
        this.isDeletingMember.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas usuwania członka:', error);
        alert('Nie udało się usunąć członka z grupy.');
        this.isDeletingMember.set(false);
      }
    });
  }

  openDeleteGroupModal(): void {
    this.showDeleteGroupModal.set(true);
  }

  closeDeleteGroupModal(): void {
    this.showDeleteGroupModal.set(false);
  }

  deleteGroup(): void {
    this.isDeletingGroup.set(true);

    this.groupService.deleteGroup(this.groupId).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: (error) => {
        console.error('Błąd podczas usuwania grupy:', error);
        alert('Nie udało się usunąć grupy.');
        this.isDeletingGroup.set(false);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}
