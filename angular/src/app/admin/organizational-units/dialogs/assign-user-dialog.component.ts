import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PrimeNGModule } from '@shared/prime-ng.module';
import { UserOrganizationalUnitService } from '@proxy/controllers/user-organizational-units.service';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-assign-user-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, PrimeNGModule],
  templateUrl: './assign-user-dialog.component.html',
  styleUrls: ['./assign-user-dialog.component.scss'],
  providers: [MessageService],
})
export class AssignUserDialogComponent implements OnInit {
  @Input() unitId: string | null = null;
  @Output() assigned = new EventEmitter<void>();
  @Output() close = new EventEmitter<void>();

  form!: FormGroup;
  isLoading = false;
  isSearching = false;
  users: any[] = [];
  roles: string[] = ['Admin', 'Manager', 'Member'];

  constructor(
    private fb: FormBuilder,
    private userOrgUnitService: UserOrganizationalUnitService,
    private messageService: MessageService
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {
    this.loadAvailableUsers();
  }

  /**
   * Initialize form
   */
  private initializeForm(): void {
    this.form = this.fb.group({
      userId: ['', Validators.required],
      roleId: ['Member', Validators.required],
    });
  }

  /**
   * Load available users (mock - replace with actual API call)
   */
  loadAvailableUsers(): void {
    this.isSearching = true;
    // TODO: Replace with actual API call to get available users
    // For now, using mock data
    this.users = [
      { id: '1', name: 'John Doe', email: 'john@example.com' },
      { id: '2', name: 'Jane Smith', email: 'jane@example.com' },
      { id: '3', name: 'Bob Johnson', email: 'bob@example.com' },
    ];
    this.isSearching = false;
  }

  /**
   * Search users by name or email
   */
  searchUsers(event: any): void {
    const query = event.query;
    if (!query || query.length < 2) {
      this.users = [];
      return;
    }

    this.isSearching = true;
    const search = query.toLowerCase();
    // TODO: Replace with actual API search
    this.users = [
      { id: '1', name: 'John Doe', email: 'john@example.com' },
      { id: '2', name: 'Jane Smith', email: 'jane@example.com' },
      { id: '3', name: 'Bob Johnson', email: 'bob@example.com' },
    ].filter(
      (u) =>
        u.name.toLowerCase().includes(search) ||
        u.email.toLowerCase().includes(search)
    );
    this.isSearching = false;
  }

  /**
   * Assign user to unit
   */
  assignUser(): void {
    if (!this.form.valid || !this.unitId) return;

    this.isLoading = true;
    const input = {
      userId: this.form.get('userId')?.value,
      roleId: this.form.get('roleId')?.value,
    };

    this.userOrgUnitService
      .assignUser(input)
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'User assigned to unit successfully',
          });
          this.assigned.emit();
        },
        error: (error) => {
          this.isLoading = false;
          console.error('Failed to assign user', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.message || 'Failed to assign user',
          });
        },
      });
  }

  /**
   * Close dialog
   */
  closeDialog(): void {
    this.close.emit();
  }

  /**
   * Format user display
   */
  formatUser(user: any): string {
    return user.name ? `${user.name} (${user.email})` : user.email;
  }
}
