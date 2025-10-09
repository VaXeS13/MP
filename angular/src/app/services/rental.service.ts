import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResultDto } from '@abp/ng.core';
import {
  RentalDto,
  CreateRentalDto,
  UpdateRentalDto,
  ExtendRentalDto,
  GetRentalListDto,
  RentalListDto,
  PaymentStatus,
  CreateRentalWithPaymentDto,
  CreateRentalWithPaymentResultDto,
  BoothCalendarRequestDto,
  BoothCalendarResponseDto
} from '../shared/models/rental.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class RentalService {
  private apiUrl = `${environment.apis.default.url}/api/app/rentals`;

  constructor(private http: HttpClient) {}

  get(id: string): Observable<RentalDto> {
    return this.http.get<RentalDto>(`${this.apiUrl}/${id}`);
  }

  getList(input: GetRentalListDto): Observable<PagedResultDto<RentalListDto>> {
    return this.http.get<PagedResultDto<RentalListDto>>(this.apiUrl, { params: input as any });
  }

  create(input: CreateRentalDto): Observable<RentalDto> {
    return this.http.post<RentalDto>(this.apiUrl, input);
  }

  createMyRental(input: CreateRentalDto): Observable<RentalDto> {
    return this.http.post<RentalDto>(`${this.apiUrl}/my-rental`, input);
  }

  createMyRentalWithPayment(input: CreateRentalWithPaymentDto): Observable<CreateRentalWithPaymentResultDto> {
    return this.http.post<CreateRentalWithPaymentResultDto>(`${this.apiUrl}/create-with-payment`, input);
  }

  update(id: string, input: UpdateRentalDto): Observable<RentalDto> {
    return this.http.put<RentalDto>(`${this.apiUrl}/${id}`, input);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  startRental(id: string): Observable<RentalDto> {
    return this.http.post<RentalDto>(`${this.apiUrl}/${id}/start`, {});
  }

  completeRental(id: string): Observable<RentalDto> {
    return this.http.post<RentalDto>(`${this.apiUrl}/${id}/complete`, {});
  }

  cancelRental(id: string, reason: string): Observable<RentalDto> {
    return this.http.post<RentalDto>(`${this.apiUrl}/${id}/cancel`, reason, {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  extendRental(id: string, input: ExtendRentalDto): Observable<RentalDto> {
    return this.http.post<RentalDto>(`${this.apiUrl}/${id}/extend`, input);
  }

  getMyRentals(input: GetRentalListDto): Observable<PagedResultDto<RentalListDto>> {
    return this.http.get<PagedResultDto<RentalListDto>>(`${this.apiUrl}/my-rentals`, { params: input as any });
  }

  getActiveRentals(): Observable<RentalListDto[]> {
    return this.http.get<RentalListDto[]>(`${this.apiUrl}/active`);
  }

  getExpiredRentals(): Observable<RentalListDto[]> {
    return this.http.get<RentalListDto[]>(`${this.apiUrl}/expired`);
  }

  getOverdueRentals(): Observable<RentalListDto[]> {
    return this.http.get<RentalListDto[]>(`${this.apiUrl}/overdue`);
  }

  getActiveRentalForBooth(boothId: string): Observable<RentalDto | null> {
    return this.http.get<RentalDto | null>(`${this.apiUrl}/active-for-booth/${boothId}`);
  }

  // Payment related methods
  initiatePayment(rentalId: string): Observable<string> {
    return this.http.post(`${this.apiUrl}/${rentalId}/initiate-payment`, {}, { responseType: 'text' });
  }

  getPaymentStatus(rentalId: string): Observable<PaymentStatus> {
    return this.http.get<PaymentStatus>(`${this.apiUrl}/${rentalId}/payment-status`);
  }

  // Availability check
  checkBoothAvailability(boothId: string, startDate: string, endDate: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}/check-availability`, {
      params: { boothId, startDate, endDate }
    });
  }

  // Calculate rental cost
  calculateRentalCost(boothId: string, boothTypeId: string, startDate: string, endDate: string): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/calculate-cost`, {
      params: { boothId, boothTypeId, startDate, endDate }
    });
  }

  // Get booth calendar data
  getBoothCalendar(input: BoothCalendarRequestDto): Observable<BoothCalendarResponseDto> {
    return this.http.post<BoothCalendarResponseDto>(`${this.apiUrl}/booth-calendar`, input);
  }
}