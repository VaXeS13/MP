import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-my-rentals',
  template: `
    <div class="my-rentals">
      <h1>Moje Wynajmy</h1>
      <p-card>
        <p-table [value]="rentals" [loading]="loading" responsiveLayout="scroll">
          <ng-template pTemplate="header">
            <tr>
              <th>Stoisko</th>
              <th>Okres</th>
              <th>Dni pozostało</th>
              <th>Przedmioty</th>
              <th>Sprzedaż</th>
              <th>Status</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-rental>
            <tr>
              <td>{{ rental.boothNumber }}</td>
              <td>{{ rental.startDate | date:'dd.MM.yyyy' }} - {{ rental.endDate | date:'dd.MM.yyyy' }}</td>
              <td>
                <p-tag [severity]="rental.isExpiringSoon ? 'warning' : 'success'"
                       [value]="rental.daysRemaining + ' dni'"></p-tag>
              </td>
              <td>{{ rental.soldItems }}/{{ rental.totalItems }}</td>
              <td>{{ formatCurrency(rental.totalSales) }}</td>
              <td>
                <p-tag [severity]="rental.status === 'Active' ? 'success' : 'info'"
                       [value]="rental.status === 'Active' ? 'Aktywny' : rental.status"></p-tag>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </p-card>
    </div>
  `,
  styles: [`
    .my-rentals {
      padding: 1rem;

      h1 {
        margin-bottom: 1.5rem;
        color: var(--primary-color);
      }
    }
  `],
  standalone: false
})
export class MyRentalsComponent implements OnInit {
  rentals: any[] = [];
  loading = false;

  ngOnInit(): void {
    this.loading = true;
    this.rentals = [
      {
        boothNumber: 'A-101',
        startDate: new Date(2025, 0, 1),
        endDate: new Date(2025, 2, 31),
        daysRemaining: 45,
        totalItems: 10,
        soldItems: 3,
        totalSales: 450.00,
        isExpiringSoon: false,
        status: 'Active'
      }
    ];
    this.loading = false;
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: 'PLN'
    }).format(amount);
  }
}
