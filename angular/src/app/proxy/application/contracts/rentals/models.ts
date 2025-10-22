
export interface PriceBreakdownDto {
  items: PriceBreakdownItemDto[];
  totalPrice: number;
  formattedBreakdown?: string;
  simpleSummary?: string;
}

export interface PriceBreakdownItemDto {
  days: number;
  count: number;
  pricePerPeriod: number;
  subtotal: number;
}
