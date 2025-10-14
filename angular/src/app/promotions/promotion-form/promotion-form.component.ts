import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PromotionService } from '../../proxy/promotions/promotion.service';
import { CreatePromotionDto, PromotionDto, UpdatePromotionDto } from '../../proxy/promotions/models';
import { PromotionType, promotionTypeOptions } from '../../proxy/promotions/promotion-type.enum';
import { DiscountType } from '../../proxy/promotions/discount-type.enum';
import { PromotionDisplayMode } from '../../proxy/promotions/promotion-display-mode.enum';
import { ToasterService } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-promotion-form',
  standalone: false,
  templateUrl: './promotion-form.component.html',
  styleUrls: ['./promotion-form.component.scss']
})
export class PromotionFormComponent implements OnInit {
  form!: FormGroup;
  promotionId?: string;
  isEditMode = false;
  loading = false;
  submitting = false;

  promotionTypeOptions = promotionTypeOptions;
  PromotionType = PromotionType;

  discountTypeOptions = [
    { label: 'Percentage', value: DiscountType.Percentage },
    { label: 'Fixed Amount', value: DiscountType.FixedAmount }
  ];

  displayModeOptions = [
    { label: 'None (Hidden)', value: PromotionDisplayMode.None },
    { label: 'Sticky Bottom Right', value: PromotionDisplayMode.StickyBottomRight },
    { label: 'Sticky Bottom Left', value: PromotionDisplayMode.StickyBottomLeft },
    { label: 'Popup', value: PromotionDisplayMode.Popup },
    { label: 'Banner', value: PromotionDisplayMode.Banner }
  ];

  constructor(
    private fb: FormBuilder,
    private promotionService: PromotionService,
    private router: Router,
    private route: ActivatedRoute,
    private toaster: ToasterService
  ) {}

  ngOnInit(): void {
    this.buildForm();

    this.promotionId = this.route.snapshot.paramMap.get('id') || undefined;
    this.isEditMode = !!this.promotionId;

    if (this.isEditMode && this.promotionId) {
      this.loadPromotion(this.promotionId);
    }

    // Watch for type changes to adjust promo code requirement
    this.form.get('type')?.valueChanges.subscribe(type => {
      const promoCodeControl = this.form.get('promoCode');
      if (type === PromotionType.PromoCode) {
        promoCodeControl?.setValidators([Validators.required, Validators.maxLength(50)]);
      } else {
        promoCodeControl?.setValidators([Validators.maxLength(50)]);
      }
      promoCodeControl?.updateValueAndValidity();
    });

    // Watch for discount type changes to adjust validation
    this.form.get('discountType')?.valueChanges.subscribe(type => {
      const discountValueControl = this.form.get('discountValue');
      if (type === DiscountType.Percentage) {
        discountValueControl?.setValidators([Validators.required, Validators.min(0), Validators.max(100)]);
      } else {
        discountValueControl?.setValidators([Validators.required, Validators.min(0)]);
      }
      discountValueControl?.updateValueAndValidity();
    });
  }

  buildForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(1000)]],
      type: [PromotionType.Quantity, [Validators.required]],
      displayMode: [PromotionDisplayMode.StickyBottomRight, [Validators.required]],
      discountType: [DiscountType.Percentage, [Validators.required]],
      discountValue: [10, [Validators.required, Validators.min(0), Validators.max(100)]],
      maxDiscountAmount: [null, [Validators.min(0)]],
      promoCode: ['', [Validators.maxLength(50)]],
      minimumBoothsCount: [null, [Validators.min(1)]],
      priority: [0, [Validators.required, Validators.min(0)]],
      validFrom: [null],
      validTo: [null],
      maxUsageCount: [null, [Validators.min(1)]],
      maxUsagePerUser: [null, [Validators.min(1)]],
      customerMessage: ['', [Validators.maxLength(500)]],
      isActive: [true],
      applicableBoothTypeIds: [[]]
    });
  }

  loadPromotion(id: string): void {
    this.loading = true;
    this.promotionService.get(id).subscribe({
      next: (promotion: PromotionDto) => {
        this.form.patchValue({
          name: promotion.name,
          description: promotion.description,
          type: promotion.type,
          displayMode: promotion.displayMode,
          discountType: promotion.discountType,
          discountValue: promotion.discountValue,
          maxDiscountAmount: promotion.maxDiscountAmount,
          promoCode: promotion.promoCode,
          minimumBoothsCount: promotion.minimumBoothsCount,
          priority: promotion.priority,
          validFrom: promotion.validFrom ? new Date(promotion.validFrom) : null,
          validTo: promotion.validTo ? new Date(promotion.validTo) : null,
          maxUsageCount: promotion.maxUsageCount,
          maxUsagePerUser: promotion.maxUsagePerUser,
          customerMessage: promotion.customerMessage,
          isActive: promotion.isActive,
          applicableBoothTypeIds: promotion.applicableBoothTypeIds || []
        });
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toaster.error('MP::PromotionLoadError');
        this.router.navigate(['/promotions']);
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      Object.keys(this.form.controls).forEach(key => {
        this.form.get(key)?.markAsTouched();
      });
      return;
    }

    this.submitting = true;
    const formValue = this.form.value;

    // Convert dates to ISO strings
    const data: CreatePromotionDto | UpdatePromotionDto = {
      ...formValue,
      validFrom: formValue.validFrom ? new Date(formValue.validFrom).toISOString() : undefined,
      validTo: formValue.validTo ? new Date(formValue.validTo).toISOString() : undefined,
      applicableBoothTypeIds: formValue.applicableBoothTypeIds || []
    };

    const request$ = this.isEditMode && this.promotionId
      ? this.promotionService.update(this.promotionId, data as UpdatePromotionDto)
      : this.promotionService.create(data as CreatePromotionDto);

    request$.subscribe({
      next: () => {
        this.submitting = false;
        this.toaster.success(
          this.isEditMode ? 'MP::PromotionUpdatedSuccessfully' : 'MP::PromotionCreatedSuccessfully'
        );
        this.router.navigate(['/promotions']);
      },
      error: () => {
        this.submitting = false;
        this.toaster.error(
          this.isEditMode ? 'MP::PromotionUpdateError' : 'MP::PromotionCreateError'
        );
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/promotions']);
  }

  get isPromoCodeRequired(): boolean {
    return this.form.get('type')?.value === PromotionType.PromoCode;
  }

  get isPercentageDiscount(): boolean {
    return this.form.get('discountType')?.value === DiscountType.Percentage;
  }
}
