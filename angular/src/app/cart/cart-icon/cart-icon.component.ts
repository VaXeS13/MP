import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-cart-icon',
  standalone: false,
  templateUrl: './cart-icon.component.html',
  styleUrl: './cart-icon.component.scss'
})
export class CartIconComponent implements OnInit {
  itemCount$: Observable<number>;

  constructor(
    public cartService: CartService,
    private router: Router
  ) {
    this.itemCount$ = new Observable(observer => {
      this.cartService.cart$.subscribe(cart => {
        observer.next(cart?.itemCount || 0);
      });
    });
  }

  ngOnInit(): void {
    // Cart is automatically loaded by CartService on initialization
  }

  navigateToCart(): void {
    this.router.navigate(['/cart']);
  }

  get itemCount(): number {
    return this.cartService.itemCount;
  }

  get totalAmount(): number {
    return this.cartService.finalAmount;
  }

  get currency(): string {
    return this.cartService.currency || 'PLN';
  }
}
