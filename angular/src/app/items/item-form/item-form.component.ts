import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { DropdownModule } from 'primeng/dropdown';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService } from 'primeng/api';
import { ItemService } from '@proxy/items';
import type { ItemDto, CreateItemDto, UpdateItemDto } from '@proxy/items/models';

@Component({
  standalone: true,
  selector: 'app-item-form',
  templateUrl: './item-form.component.html',
  styleUrls: ['./item-form.component.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    DialogModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    DropdownModule,
    CheckboxModule
  ]
})
export class ItemFormComponent implements OnInit {
  @Input() visible = false;
  @Input() item: ItemDto | null = null;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() saved = new EventEmitter<void>();

  form!: FormGroup;
  saving = false;
  createAnother = false;

  currencies = [
    { label: 'PLN', value: 'PLN' },
    { label: 'EUR', value: 'EUR' },
    { label: 'USD', value: 'USD' },
    { label: 'GBP', value: 'GBP' },
    { label: 'CZK', value: 'CZK' }
  ];

  constructor(
    private fb: FormBuilder,
    private itemService: ItemService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.buildForm();
  }

  ngOnChanges(): void {
    if (this.form && this.item) {
      this.form.patchValue({
        name: this.item.name,
        category: this.item.category,
        price: this.item.price,
        currency: this.item.currency
      });
    } else if (this.form) {
      this.form.reset({ currency: 'PLN' });
    }
  }

  buildForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      category: ['', Validators.maxLength(100)],
      price: [null, [Validators.required, Validators.min(0.01)]],
      currency: ['PLN', Validators.required]
    });
  }

  onHide(): void {
    this.visible = false;
    this.visibleChange.emit(false);
    this.form.reset({ currency: 'PLN' });
    this.createAnother = false;
  }

  save(): void {
    if (this.form.invalid) {
      Object.keys(this.form.controls).forEach(key => {
        this.form.controls[key].markAsTouched();
      });
      return;
    }

    this.saving = true;
    const formValue = this.form.value;

    if (this.item?.id) {
      // Update
      const updateDto: UpdateItemDto = {
        name: formValue.name,
        category: formValue.category || undefined,
        price: formValue.price,
        currency: formValue.currency
      };

      this.itemService.update(this.item.id, updateDto).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Item updated successfully'
          });
          this.saving = false;
          this.saved.emit();
          this.onHide();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.error?.message || 'Failed to update item'
          });
          this.saving = false;
        }
      });
    } else {
      // Create
      const createDto: CreateItemDto = {
        name: formValue.name,
        category: formValue.category || undefined,
        price: formValue.price,
        currency: formValue.currency
      };

      this.itemService.create(createDto).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Item created successfully'
          });
          this.saving = false;
          this.saved.emit();

          if (this.createAnother) {
            // Keep dialog open and reset only name field
            this.form.patchValue({ name: '' });
            this.form.get('name')?.markAsUntouched();
          } else {
            this.onHide();
          }
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.error?.message || 'Failed to create item'
          });
          this.saving = false;
        }
      });
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getFieldError(fieldName: string): string {
    const field = this.form.get(fieldName);
    if (field?.hasError('required')) {
      return 'This field is required';
    }
    if (field?.hasError('maxlength')) {
      return `Maximum length is ${field.errors?.['maxlength'].requiredLength}`;
    }
    if (field?.hasError('min')) {
      return 'Price must be greater than 0';
    }
    return '';
  }
}
