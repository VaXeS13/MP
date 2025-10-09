export interface RentalDto {
  id: string;
  userId: string;
  boothId: string;
  boothTypeId: string;
  startDate: string;
  endDate: string;
  totalCost: number;
  status: RentalStatus;
  paymentStatus: PaymentStatus;
  notes?: string;
  startedAt?: string;
  completedAt?: string;
  creationTime: string;

  // Navigation properties
  booth?: {
    number: string;
    price: number;
    currency: string;
  };
  boothType?: {
    name: string;
    commissionPercentage: number;
  };
}

export interface CreateRentalDto {
  boothId: string;
  boothTypeId?: string;
  startDate: Date | string; // Date object or YYYY-MM-DD format
  endDate: Date | string;   // Date object or YYYY-MM-DD format
  notes?: string;
}

export interface UpdateRentalDto {
  notes?: string;
}

export interface ExtendRentalDto {
  rentalId: string;
  newEndDate: string; // YYYY-MM-DD format
  paymentType: ExtensionPaymentType;
  terminalTransactionId?: string;
  terminalReceiptNumber?: string;
  onlineTimeoutMinutes?: number;
}

export enum ExtensionPaymentType {
  Free = 0,      // Gratis - free of charge, instant
  Cash = 1,      // Cash payment on-site, instant
  Terminal = 2,  // Card payment via terminal with transaction ID and receipt
  Online = 3     // Add to cart with timeout, skip gap validation
}

export interface GetRentalListDto {
  skipCount: number;
  maxResultCount: number;
  filter?: string;
  sorting?: string;
  status?: RentalStatus;
  boothId?: string;
  userId?: string;
}

export interface RentalListDto {
  id: string;
  boothNumber: string;
  boothTypeName: string;
  startDate: string;
  endDate: string;
  totalCost: number;
  status: RentalStatus;
  paymentStatus: PaymentStatus;
  daysCount: number;
  creationTime: string;
}

export enum RentalStatus {
  Draft = 0,
  Active = 1,
  Extended = 2,
  Expired = 3,
  Cancelled = 4
}

export enum PaymentStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
  Cancelled = 4,
  Refunded = 5
}

export interface CreateRentalWithPaymentDto {
  boothId: string;
  boothTypeId: string;
  startDate: string; // YYYY-MM-DD format
  endDate: string;   // YYYY-MM-DD format
  notes?: string;
  paymentProviderId: string;
  paymentMethodId?: string;
}

export interface CreateRentalWithPaymentResultDto {
  success: boolean;
  rentalId?: string;
  transactionId?: string;
  paymentUrl?: string;
  errorMessage?: string;
}

// Calendar interfaces
export interface BoothCalendarRequestDto {
  boothId: string;
  startDate: string; // YYYY-MM-DD format
  endDate: string;   // YYYY-MM-DD format
  excludeCartId?: string; // Optional: exclude items from this cart (for editing)
}

export interface BoothCalendarResponseDto {
  boothId: string;
  boothNumber: string;
  startDate: string;
  endDate: string;
  dates: CalendarDateDto[];
  legend: { [key: string]: string };
}

export interface CalendarDateDto {
  date: string; // YYYY-MM-DD format
  status: CalendarDateStatus;
  statusDisplayName: string;
  rentalId?: string;
  userName?: string;
  userEmail?: string;
  rentalStartDate?: string;
  rentalEndDate?: string;
  notes?: string;
}

export enum CalendarDateStatus {
  Available = 0,
  Reserved = 1,
  Occupied = 2,
  Unavailable = 3,
  PastDate = 4,
  Historical = 5
}