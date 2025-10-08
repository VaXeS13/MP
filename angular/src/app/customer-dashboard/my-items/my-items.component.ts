import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-my-items',
  templateUrl: './my-items.component.html',
  styleUrls: ['./my-items.component.scss'],
  standalone: false
})
export class MyItemsComponent implements OnInit {
  items: any[] = [];
  loading = false;
  displayDialog = false;
  selectedItem: any = {};
  isEditMode = false;

  statuses = [
    { label: 'Na sprzedaż', value: 'ForSale' },
    { label: 'Sprzedane', value: 'Sold' },
    { label: 'Odebrane', value: 'Reclaimed' }
  ];

  constructor() {}

  ngOnInit(): void {
    this.loadItems();
  }

  loadItems(): void {
    this.loading = true;

    // Mock data - replace with actual API call
    this.items = [
      {
        id: '1',
        name: 'Vintage Lamp',
        category: 'Lighting',
        estimatedPrice: 150.00,
        actualPrice: 150.00,
        status: 'Sold',
        boothNumber: 'A-101',
        soldAt: new Date(2025, 0, 15),
        creationTime: new Date(2025, 0, 1)
      },
      {
        id: '2',
        name: 'Wooden Chair',
        category: 'Furniture',
        estimatedPrice: 120.00,
        status: 'ForSale',
        boothNumber: 'A-101',
        creationTime: new Date(2025, 0, 5)
      },
      {
        id: '3',
        name: 'Antique Clock',
        category: 'Decor',
        estimatedPrice: 250.00,
        status: 'ForSale',
        boothNumber: 'B-205',
        creationTime: new Date(2025, 1, 1)
      }
    ];

    setTimeout(() => {
      this.loading = false;
    }, 500);
  }

  addNewItem(): void {
    this.selectedItem = {
      name: '',
      category: '',
      estimatedPrice: 0,
      description: ''
    };
    this.isEditMode = false;
    this.displayDialog = true;
  }

  editItem(item: any): void {
    this.selectedItem = { ...item };
    this.isEditMode = true;
    this.displayDialog = true;
  }

  deleteItem(item: any): void {
    if (confirm(`Czy na pewno chcesz usunąć przedmiot "${item.name}"?`)) {
      this.items = this.items.filter(i => i.id !== item.id);
    }
  }

  saveItem(): void {
    if (this.isEditMode) {
      const index = this.items.findIndex(i => i.id === this.selectedItem.id);
      if (index !== -1) {
        this.items[index] = { ...this.selectedItem };
      }
    } else {
      this.items.push({
        ...this.selectedItem,
        id: Date.now().toString(),
        status: 'ForSale',
        creationTime: new Date()
      });
    }

    this.displayDialog = false;
  }

  getStatusSeverity(status: string): string {
    switch (status) {
      case 'ForSale': return 'info';
      case 'Sold': return 'success';
      case 'Reclaimed': return 'warning';
      default: return 'secondary';
    }
  }

  getStatusLabel(status: string): string {
    const found = this.statuses.find(s => s.value === status);
    return found ? found.label : status;
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: 'PLN'
    }).format(amount || 0);
  }
}
