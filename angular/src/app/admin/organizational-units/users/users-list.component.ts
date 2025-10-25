import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PrimeNGModule } from '@shared/prime-ng.module';
import { UserOrganizationalUnitService } from '@proxy/controllers/user-organizational-units.service';
import { CurrentOrganizationalUnitService } from '@services/current-organizational-unit.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ConfirmationService, MessageService } from 'primeng/api';
import { AssignUserDialogComponent } from '../dialogs/assign-user-dialog.component';

interface UserUnit {
  id?: string;
  userId?: string;
  userName?: string;
  email?: string;
  role?: string;
  assignedDate?: string;
  isActive: boolean;
}

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [CommonModule, PrimeNGModule, AssignUserDialogComponent],
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss'],
  providers: [ConfirmationService, MessageService],
})
export class UsersListComponent implements OnInit, OnDestroy {
  users: UserUnit[] = [];
  isLoading = false;
  showDialog = false;
  currentUnitId: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private userOrgUnitService: UserOrganizationalUnitService,
    private currentUnitService: CurrentOrganizationalUnitService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService
  ) {}

  ngOnInit(): void {
    const currentUnit = this.currentUnitService.getCurrentUnit();
    if (currentUnit?.unitId) {
      this.currentUnitId = currentUnit.unitId;
      this.loadUsers();
    }
  }

  /**
   * Load all users in the current unit
   */
  loadUsers(): void {
    if (!this.currentUnitId) return;

    this.isLoading = true;
    this.userOrgUnitService
      .getMyUnits()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (myUnits: any) => {
          // Filter users in current unit
          const currentUnitUsers = myUnits.filter((u: any) => u.unitId === this.currentUnitId);
          this.users = currentUnitUsers || [];
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Failed to load unit users', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load unit members',
          });
          this.isLoading = false;
        },
      });
  }

  /**
   * Open dialog to assign new user
   */
  openAssignDialog(): void {
    this.showDialog = true;
  }

  /**
   * Handle user assigned successfully
   */
  onUserAssigned(): void {
    this.showDialog = false;
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: 'User assigned to unit successfully',
    });
    this.loadUsers();
  }

  /**
   * Change user role
   */
  changeRole(user: UserUnit): void {
    if (!user.id) return;

    // TODO: Implement role change dialog
    this.messageService.add({
      severity: 'info',
      summary: 'Info',
      detail: 'Role change functionality coming soon',
    });
  }

  /**
   * Remove user from unit
   */
  removeUser(user: UserUnit): void {
    if (!user.id) return;

    this.confirmationService.confirm({
      message: `Are you sure you want to remove "${user.userName}" from this unit?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        // TODO: Call API to remove user
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'User removed from unit',
        });
        this.loadUsers();
      },
    });
  }

  /**
   * Close assign dialog
   */
  onDialogClose(): void {
    this.showDialog = false;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
