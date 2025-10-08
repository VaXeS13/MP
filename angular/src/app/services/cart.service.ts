import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { CartDto, AddToCartDto, UpdateCartItemDto, CheckoutCartDto, CheckoutResultDto } from '../shared/models/cart.model';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private apiUrl = `${environment.apis.default.url}/api/app/cart`;

  // Cart state management with RxJS
  private cartSubject = new BehaviorSubject<CartDto | null>(null);
  public cart$ = this.cartSubject.asObservable();

  private loadingSubject = new BehaviorSubject<boolean>(false);
  public loading$ = this.loadingSubject.asObservable();

  constructor(private http: HttpClient) {
    // Load cart on service initialization
    this.loadCart();
  }

  /**
   * Get current cart value (synchronous)
   */
  get currentCart(): CartDto | null {
    return this.cartSubject.value;
  }

  /**
   * Get cart item count
   */
  get itemCount(): number {
    return this.cartSubject.value?.itemCount || 0;
  }

  /**
   * Get cart total amount
   */
  get totalAmount(): number {
    return this.cartSubject.value?.totalAmount || 0;
  }

  /**
   * Get cart currency (from first item)
   */
  get currency(): string {
    const cart = this.cartSubject.value;
    if (!cart || !cart.items || cart.items.length === 0) {
      return 'PLN';
    }
    return cart.items[0].currency || 'PLN';
  }

  /**
   * Load user's cart from API
   */
  loadCart(): void {
    this.loadingSubject.next(true);
    this.http.get<CartDto>(`${this.apiUrl}/my-cart`).subscribe({
      next: (cart) => {
        this.cartSubject.next(cart);
        this.loadingSubject.next(false);
      },
      error: (error) => {
        console.error('Error loading cart:', error);
        this.loadingSubject.next(false);
      }
    });
  }

  /**
   * Get user's cart (returns observable)
   */
  getMyCart(): Observable<CartDto> {
    return this.http.get<CartDto>(`${this.apiUrl}/my-cart`).pipe(
      tap(cart => this.cartSubject.next(cart))
    );
  }

  /**
   * Add item to cart
   */
  addItem(item: AddToCartDto): Observable<CartDto> {
    return this.http.post<CartDto>(`${this.apiUrl}/item`, item).pipe(
      tap(cart => this.cartSubject.next(cart))
    );
  }

  /**
   * Update cart item
   */
  updateItem(itemId: string, item: UpdateCartItemDto): Observable<CartDto> {
    return this.http.put<CartDto>(`${this.apiUrl}/item/${itemId}`, item).pipe(
      tap(cart => this.cartSubject.next(cart))
    );
  }

  /**
   * Remove item from cart
   */
  removeItem(itemId: string): Observable<CartDto> {
    return this.http.delete<CartDto>(`${this.apiUrl}/item/${itemId}`).pipe(
      tap(cart => this.cartSubject.next(cart))
    );
  }

  /**
   * Clear entire cart
   */
  clearCart(): Observable<CartDto> {
    return this.http.post<CartDto>(`${this.apiUrl}/clear`, {}).pipe(
      tap(cart => this.cartSubject.next(cart))
    );
  }

  /**
   * Checkout cart
   */
  checkout(checkoutDto: CheckoutCartDto): Observable<CheckoutResultDto> {
    return this.http.post<CheckoutResultDto>(`${this.apiUrl}/checkout`, checkoutDto).pipe(
      tap(result => {
        if (result.success) {
          // Clear cart after successful checkout
          this.cartSubject.next(null);
        }
      })
    );
  }

  /**
   * Check if booth is already in cart
   */
  hasBoothInCart(boothId: string): boolean {
    const cart = this.cartSubject.value;
    return cart?.items.some(item => item.boothId === boothId) || false;
  }

  /**
   * Check if booth has overlapping dates in cart
   */
  hasOverlappingBooking(boothId: string, startDate: Date, endDate: Date): boolean {
    const cart = this.cartSubject.value;
    if (!cart) return false;

    return cart.items.some(item => {
      if (item.boothId !== boothId) return false;

      const itemStart = new Date(item.startDate);
      const itemEnd = new Date(item.endDate);

      return startDate <= itemEnd && endDate >= itemStart;
    });
  }

  /**
   * Refresh cart from server
   */
  refresh(): void {
    this.loadCart();
  }
}