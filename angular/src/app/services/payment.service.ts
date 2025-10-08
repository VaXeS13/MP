import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import {
  PaymentProvider,
  PaymentMethod,
  PaymentRequest,
  PaymentResponse,
  GetPaymentProvidersResponse,
  GetPaymentMethodsRequest,
  GetPaymentMethodsResponse
} from '../shared/models/payment.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private apiUrl = `${environment.apis.default.url}/api/app/payments`;

  constructor(private http: HttpClient) {}

  // Get available payment providers
  getPaymentProviders(): Observable<PaymentProvider[]> {
    console.log('PaymentService: Making request to', `${this.apiUrl}/providers`);
    return this.http.get<PaymentProvider[]>(`${this.apiUrl}/providers`);
  }

  // Get payment methods for specific provider
  getPaymentMethods(providerId: string, currency: string = 'PLN'): Observable<PaymentMethod[]> {
    return this.http.get<PaymentMethod[]>(`${this.apiUrl}/providers/${providerId}/methods?currency=${currency}`);
  }

  // Create payment transaction
  createPayment(request: PaymentRequest): Observable<PaymentResponse> {
    console.log('PaymentService: createPayment called with request:', request);
    console.log('PaymentService: Making POST request to:', `${this.apiUrl}/create`);

    return this.http.post<PaymentResponse>(`${this.apiUrl}/create`, request).pipe(
      tap(response => console.log('PaymentService: Received response:', response)),
      tap(response => {
        if (!response || !response.success) {
          console.error('PaymentService: Payment creation failed:', response);
        }
      })
    );
  }

  // Verify payment status
  verifyPayment(transactionId: string): Observable<PaymentResponse> {
    return this.http.get<PaymentResponse>(`${this.apiUrl}/verify/${transactionId}`);
  }
}