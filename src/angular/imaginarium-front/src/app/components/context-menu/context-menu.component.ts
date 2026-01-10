import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ContextMenuItem {
  label: string;
  icon?: string;
  action: () => void;
  disabled?: boolean;
}

@Component({
  selector: 'app-context-menu',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './context-menu.component.html',
  styleUrl: './context-menu.component.css'
})
export class ContextMenuComponent {
  @Input() items: ContextMenuItem[] = [];
  @Input() x: number = 0;
  @Input() y: number = 0;
  @Input() visible: boolean = false;
  @Output() close = new EventEmitter<void>();

  @HostListener('document:click', ['$event'])
  onClickOutside(event: MouseEvent): void {
    if (this.visible) {
      const target = event.target as HTMLElement;
      if (!target.closest('.context-menu')) {
        this.closeMenu();
      }
    }
  }

  @HostListener('document:contextmenu', ['$event'])
  onRightClick(event: MouseEvent): void {
    if (this.visible) {
      event.preventDefault();
      this.closeMenu();
    }
  }

  closeMenu(): void {
    this.visible = false;
    this.close.emit();
  }

  handleItemClick(item: ContextMenuItem): void {
    if (!item.disabled) {
      item.action();
      this.closeMenu();
    }
  }
}
