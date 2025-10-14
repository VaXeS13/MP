import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { tap, catchError, shareReplay } from 'rxjs/operators';
import { TenantCurrencyService as TenantCurrencyApiService } from '../proxy/tenants/tenant-currency.service';
import { TenantCurrencyDto } from '../proxy/tenants/models';
import { Currency } from '../proxy/domain/booths/currency.enum';

@Injectable({
  providedIn: 'root',
})
export class TenantCurrencyService {
  private currencySubject = new BehaviorSubject<Currency>(Currency.PLN);
  public currency$ = this.currencySubject.asObservable();

  private currencyCache$: Observable<TenantCurrencyDto> | null = null;

  constructor(private tenantCurrencyApi: TenantCurrencyApiService) {}

  /**
   * Get tenant currency (cached)
   */
  getCurrency(): Observable<TenantCurrencyDto> {
    if (!this.currencyCache$) {
      this.currencyCache$ = this.tenantCurrencyApi.getTenantCurrency().pipe(
        tap(result => {
          if (result.currency) {
            this.currencySubject.next(result.currency);
          }
        }),
        catchError(() => {
          // If error, return default PLN
          this.currencySubject.next(Currency.PLN);
          return of({ currency: Currency.PLN });
        }),
        shareReplay(1)
      );
    }

    return this.currencyCache$;
  }

  /**
   * Get currency code as string (e.g., "PLN", "EUR")
   */
  getCurrencyCode(): string {
    return this.getCurrencyName(this.currencySubject.value);
  }

  /**
   * Refresh currency from server
   */
  refresh(): Observable<TenantCurrencyDto> {
    this.currencyCache$ = null;
    return this.getCurrency();
  }

  /**
   * Get currency display name
   */
  getCurrencyName(currency?: Currency): string {
    switch (currency) {
      case Currency.PLN:
        return 'PLN';
      case Currency.EUR:
        return 'EUR';
      case Currency.USD:
        return 'USD';
      case Currency.GBP:
        return 'GBP';
      case Currency.CZK:
        return 'CZK';
      default:
        return 'PLN';
    }
  }
}
