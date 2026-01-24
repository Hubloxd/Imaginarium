import { Component, signal, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ShareService, CreateShareDto, Permission, Share } from '../../services/share.service';
import { GroupService, Group } from '../../services/group.service';

@Component({
  selector: 'app-share-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './share-modal.component.html',
  styleUrl: './share-modal.component.css'
})
export class ShareModalComponent implements OnInit, OnChanges {
  @Input() mediaId?: string;
  @Input() albumId?: string;
  @Input() visible: boolean = false;
  @Output() close = new EventEmitter<void>();
  @Output() shared = new EventEmitter<void>();

  shareType = signal<'user' | 'group'>('user');
  selectedUserEmail = signal<string>('');
  selectedGroupId = signal<string>('');
  permissionLevel = signal<Permission>(Permission.View);
  isPublic = signal<boolean>(false);
  expiresAt = signal<string>('');

  groups = signal<Group[]>([]);
  isLoadingGroups = signal<boolean>(false);
  isSharing = signal<boolean>(false);
  createdShare = signal<Share | null>(null);
  publicLink = signal<string>('');
  linkCopied = signal<boolean>(false);

  Permission = Permission;

  constructor(
    private shareService: ShareService,
    private groupService: GroupService
  ) {}

  ngOnInit(): void {
    if (this.visible) {
      this.loadGroups();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && changes['visible'].currentValue) {
      this.loadGroups();
      this.resetForm();
    }
  }

  loadGroups(): void {
    this.isLoadingGroups.set(true);
    this.groupService.getGroups().subscribe({
      next: (groups) => {
        this.groups.set(groups);
        this.isLoadingGroups.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas pobierania grup:', error);
        this.isLoadingGroups.set(false);
      }
    });
  }

  resetForm(): void {
    this.shareType.set('user');
    this.selectedUserEmail.set('');
    this.selectedGroupId.set('');
    this.permissionLevel.set(Permission.View);
    this.isPublic.set(false);
    this.expiresAt.set('');
    this.createdShare.set(null);
    this.publicLink.set('');
    this.linkCopied.set(false);
  }

  closeModal(): void {
    this.visible = false;
    this.close.emit();
  }

  share(): void {
    // Jeśli publiczne, nie wymagaj użytkownika/grupy
    if (!this.isPublic()) {
      if (this.shareType() === 'user' && !this.selectedUserEmail().trim()) {
        alert('Podaj email użytkownika');
        return;
      }

      if (this.shareType() === 'group' && !this.selectedGroupId()) {
        alert('Wybierz grupę');
        return;
      }
    }

    this.isSharing.set(true);

    const dto: CreateShareDto = {
      mediaId: this.mediaId,
      albumId: this.albumId,
      sharedWithUserEmail: this.shareType() === 'user' ? this.selectedUserEmail().trim() : undefined,
      sharedWithGroupId: this.shareType() === 'group' ? this.selectedGroupId() : undefined,
      isPublic: this.isPublic(),
      expiresAt: this.expiresAt() || undefined,
      permissionLevel: this.permissionLevel() as Permission
    };

    this.shareService.createShare(dto).subscribe({
      next: (share) => {
        this.createdShare.set(share);
        // Jeśli to publiczne udostępnienie, wygeneruj link
        if (share.isPublic && share.shareToken) {
          const baseUrl = window.location.origin;
          this.publicLink.set(`${baseUrl}/share/${share.shareToken}`);
        } else {
          // Jeśli nie publiczne, zamknij modal
          this.closeModal();
          this.shared.emit();
        }
        this.isSharing.set(false);
      },
      error: (error) => {
        console.error('Błąd podczas udostępniania:', error);
        alert('Nie udało się udostępnić. Sprawdź czy użytkownik/grupa istnieje i czy nie jest już udostępnione.');
        this.isSharing.set(false);
      }
    });
  }

  getPermissionName(permission: Permission): string {
    switch (permission) {
      case Permission.View:
        return 'Przeglądanie';
      case Permission.Download:
        return 'Pobieranie';
      case Permission.Edit:
        return 'Edycja';
      default:
        return '';
    }
  }

  copyLink(): void {
    const link = this.publicLink();
    if (link) {
      navigator.clipboard.writeText(link).then(() => {
        this.linkCopied.set(true);
        setTimeout(() => {
          this.linkCopied.set(false);
        }, 2000);
      }).catch(err => {
        console.error('Błąd podczas kopiowania linku:', err);
        alert('Nie udało się skopiować linku');
      });
    }
  }

  closeAfterShare(): void {
    this.closeModal();
    this.shared.emit();
  }
}
