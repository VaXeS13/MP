import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { PagedResultDto, PagedAndSortedResultRequestDto, FullAuditedEntityDto, Rest } from '@abp/ng.core';

export interface GetPaymentTransactionListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  status?: string;
  paymentMethod?: string;
  startDate?: Date;
  endDate?: Date;
  minAmount?: number;
  maxAmount?: number;
  rentalId?: string;
  email?: string;
}

export interface PaymentTransactionDto extends FullAuditedEntityDto<string> {
  sessionId: string;
  merchantId: number;
  posId: number;
  amount: number;
  currency: string;
  email: string;
  description: string;
  method?: string;
  transferLabel?: string;
  sign: string;
  orderId?: string;
  verified: boolean;
  returnUrl?: string;
  statement?: string;
  extraProperties?: string;
  manualStatusCheckCount: number;
  status: string;
  lastStatusCheck?: string;
  rentalId?: string;
  statusDisplayName?: string;
  isCompleted: boolean;
  isFailed: boolean;
  isPending: boolean;
  isVerified: boolean;
  formattedAmount?: string;
  formattedCreatedAt?: string;
  formattedCompletedAt?: string;
  paymentMethodDisplayName?: string;
}

export interface CreatePaymentTransactionDto {
  sessionId: string;
  merchantId: number;
  posId: number;
  amount: number;
  currency: string;
  email: string;
  description: string;
  method?: string;
  transferLabel?: string;
  sign: string;
  orderId?: string;
  returnUrl?: string;
  statement?: string;
  extraProperties?: string;
  rentalId?: string;
}

export interface UpdatePaymentTransactionDto {
  description?: string;
  status?: string;
  lastStatusCheck?: string;
  method?: string;
  transferLabel?: string;
  returnUrl?: string;
  statement?: string;
  extraProperties?: string;
  verified?: boolean;
  manualStatusCheckCount?: number;
}

export interface RentalSummaryDto {
  id: string;
  boothNumber: string;
  startDate: string;
  endDate: string;
  daysCount: number;
  totalAmount: number;
  currency: string;
  status: string;
  statusDisplayName: string;
  formattedAmount?: string;
  formattedDateRange?: string;
}

export interface PaymentSuccessViewModel {
  transaction: PaymentTransactionDto;
  rentals: RentalSummaryDto[];
  success: boolean;
  message: string;
  nextStepUrl: string;
  nextStepText: string;
  totalAmount: number;
  currency: string;
  paymentDate: string;
  paymentMethod: string;
  formattedPaymentDate: string;
  formattedTotalAmount: string;
  orderId?: string;
  paymentProvider: string;
  isVerified: boolean;
  method?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentTransactionService {
  private readonly apiUrl = '/api/app/payment-transactions';

  constructor(private http: HttpClient) {
  }

  getList(input: GetPaymentTransactionListInput = { maxResultCount: 10, skipCount: 0 }): Observable<PagedResultDto<PaymentTransactionDto>> {
    let params = new HttpParams();

    if (input.filter) params = params.append('filter', input.filter);
    if (input.status) params = params.append('status', input.status);
    if (input.paymentMethod) params = params.append('paymentMethod', input.paymentMethod);
    if (input.startDate) params = params.append('startDate', input.startDate.toISOString());
    if (input.endDate) params = params.append('endDate', input.endDate.toISOString());
    if (input.minAmount) params = params.append('minAmount', input.minAmount.toString());
    if (input.maxAmount) params = params.append('maxAmount', input.maxAmount.toString());
    if (input.rentalId) params = params.append('rentalId', input.rentalId);
    if (input.email) params = params.append('email', input.email);
    if (input.sorting) params = params.append('sorting', input.sorting);
    if (input.skipCount) params = params.append('skipCount', input.skipCount.toString());
    if (input.maxResultCount) params = params.append('maxResultCount', input.maxResultCount.toString());

    return this.http.get<PagedResultDto<PaymentTransactionDto>>(this.apiUrl, { params });
  }

  get(id: string): Observable<PaymentTransactionDto> {
    return this.http.get<PaymentTransactionDto>(this.apiUrl + '/' + id);
  }

  create(input: CreatePaymentTransactionDto): Observable<PaymentTransactionDto> {
    return this.http.post<PaymentTransactionDto>(this.apiUrl, input);
  }

  update(id: string, input: UpdatePaymentTransactionDto): Observable<PaymentTransactionDto> {
    return this.http.put<PaymentTransactionDto>(this.apiUrl + '/' + id, input);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(this.apiUrl + '/' + id);
  }

  getPaymentSuccessViewModel(transactionId: string): Observable<PaymentSuccessViewModel> {
    return this.http.get<PaymentSuccessViewModel>(this.apiUrl + '/payment-success/' + transactionId);
  }

  getBySessionId(sessionId: string): Observable<PaymentTransactionDto | null> {
    return this.getPaymentSuccessViewModel(sessionId).pipe(
      map(viewModel => viewModel.transaction || null)
    );
  }
}