import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Przelewy24Settings {
  enabled: boolean;
  merchantId?: string;
  posId?: string;
  apiKey?: string;
  crcKey?: string;
  isSandbox: boolean;
}

export interface PayPalSettings {
  enabled: boolean;
  clientId?: string;
  clientSecret?: string;
  isSandbox: boolean;
}

export interface StripeSettings {
  enabled: boolean;
  publishableKey?: string;
  secretKey?: string;
  webhookSecret?: string;
}

export interface PaymentProviderSettings {
  przelewy24: Przelewy24Settings;
  payPal: PayPalSettings;
  stripe: StripeSettings;
}

export interface UpdatePaymentProviderSettings {
  przelewy24: Przelewy24Settings;
  payPal: PayPalSettings;
  stripe: StripeSettings;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentProviderSettingsService {
  private apiUrl = `${environment.apis.default.url}/api/app/payment-provider-settings`; 

  constructor(private http: HttpClient) {}

  getSettings(): Observable<PaymentProviderSettings> {
    return this.http.get<PaymentProviderSettings>(this.apiUrl);
  }

  updateSettings(settings: UpdatePaymentProviderSettings): Observable<void> {
    return this.http.put<void>(this.apiUrl, settings);
  }
}
