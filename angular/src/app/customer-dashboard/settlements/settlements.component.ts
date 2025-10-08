import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-settlements',
  template: `
    <div class="settlements">
      <h1>Rozliczenia</h1>

      <div class="grid mb-3">
        <div class="col-12 md:col-4">
          <p-card>
            <div class="stat-card">
              <i class="pi pi-money-bill stat-icon"></i>
              <div class="stat-content">
                <span class="stat-label">Całkowite zarobki</span>
                <span class="stat-value">{{ formatCurrency(1250.00) }}</span>
              </div>
            </div>
          </p-card>
        </div>
        <div class="col-12 md:col-4">
          <p-card>
            <div class="stat-card">
              <i class="pi pi-wallet stat-icon"></i>
              <div class="stat-content">
                <span class="stat-label">Do wypłaty</span>
                <span class="stat-value">{{ formatCurrency(950.00) }}</span>
              </div>
            </div>
          </p-card>
        </div>
        <div class="col-12 md:col-4">
          <p-card>
            <div class="stat-card">
              <i class="pi pi-clock stat-icon"></i>
              <div class="stat-content">
                <span class="stat-label">W trakcie</span>
                <span class="stat-value">{{ formatCurrency(0) }}</span>
              </div>
            </div>
          </p-card>
        </div>
      </div>

      <p-card>
        <ng-template pTemplate="header">
          <div class="flex justify-content-between align-items-center p-3">
            <h3>Historia rozliczeń</h3>
            <button pButton label="Zgłoś wypłatę" icon="pi pi-send" class="p-button-success"></button>
          </div>
        </ng-template>
        <p-table [value]="settlements" [loading]="loading" responsiveLayout="scroll">
          <ng-template pTemplate="header">
            <tr>
              <th>Numer</th>
              <th>Data</th>
              <th>Kwota</th>
              <th>Przedmioty</th>
              <th>Status</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-settlement>
            <tr>
              <td>{{ settlement.number }}</td>
              <td>{{ settlement.date | date:'dd.MM.yyyy' }}</td>
              <td>{{ formatCurrency(settlement.amount) }}</td>
              <td>{{ settlement.itemsCount }}</td>
              <td>
                <p-tag [severity]="settlement.status === 'Completed' ? 'success' : 'warning'"
                       [value]="settlement.status === 'Completed' ? 'Wypłacono' : 'W trakcie'"></p-tag>
              </td>
            </tr>
          </ng-template>
          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="5" class="text-center">Brak rozliczeń</td>
            </tr>
          </ng-template>
        </p-table>
      </p-card>
    </div>
  `,
  styles: [`
    .settlements {
      padding: 1rem;

      h1 {
        margin-bottom: 1.5rem;
        color: var(--primary-color);
      }

      .stat-card {
        display: flex;
        align-items: center;
        gap: 1rem;

        .stat-icon {
          font-size: 2rem;
          color: var(--primary-color);
        }

        .stat-content {
          display: flex;
          flex-direction: column;

          .stat-label {
            font-size: 0.875rem;
            color: var(--text-color-secondary);
          }

          .stat-value {
            font-size: 1.5rem;
            font-weight: 600;
          }
        }
      }

      .text-center {
        text-align: center;
      }
    }
  `],
  standalone: false
})
export class SettlementsComponent implements OnInit {
  settlements: any[] = [];
  loading = false;

  ngOnInit(): void {
    this.loading = true;
    this.settlements = [];
    this.loading = false;
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: 'PLN'
    }).format(amount);
  }
}
