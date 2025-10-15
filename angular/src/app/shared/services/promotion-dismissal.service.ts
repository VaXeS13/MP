import { Injectable } from '@angular/core';

interface DismissedPromotion {
  promotionId: string;
  dismissedAt: number; // timestamp in milliseconds
}

@Injectable({
  providedIn: 'root'
})
export class PromotionDismissalService {
  private readonly STORAGE_KEY = 'dismissed_promotions';
  private readonly DISMISSAL_DURATION_MS = 60 * 60 * 1000; // 1 hour in milliseconds

  constructor() {}

  /**
   * Mark a promotion as dismissed for the configured duration
   */
  dismissPromotion(promotionId: string): void {
    const dismissedPromotions = this.getDismissedPromotions();
    const now = Date.now();

    // Remove any existing entry for this promotion
    const filtered = dismissedPromotions.filter(p => p.promotionId !== promotionId);

    // Add new dismissal entry
    filtered.push({
      promotionId,
      dismissedAt: now
    });

    this.saveDismissedPromotions(filtered);
  }

  /**
   * Check if a promotion is currently dismissed
   */
  isPromotionDismissed(promotionId: string): boolean {
    const dismissedPromotions = this.getDismissedPromotions();
    const now = Date.now();

    const dismissal = dismissedPromotions.find(p => p.promotionId === promotionId);

    if (!dismissal) {
      return false;
    }

    // Check if dismissal has expired
    const timeSinceDismissal = now - dismissal.dismissedAt;
    if (timeSinceDismissal >= this.DISMISSAL_DURATION_MS) {
      // Dismissal has expired, remove it
      this.removeDismissedPromotion(promotionId);
      return false;
    }

    return true;
  }

  /**
   * Get all dismissed promotions from localStorage and clean up expired entries
   */
  private getDismissedPromotions(): DismissedPromotion[] {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      if (!stored) {
        return [];
      }

      const dismissed: DismissedPromotion[] = JSON.parse(stored);
      const now = Date.now();

      // Filter out expired dismissals
      const active = dismissed.filter(p => {
        const timeSinceDismissal = now - p.dismissedAt;
        return timeSinceDismissal < this.DISMISSAL_DURATION_MS;
      });

      // Save cleaned up list if any were removed
      if (active.length !== dismissed.length) {
        this.saveDismissedPromotions(active);
      }

      return active;
    } catch (error) {
      console.error('Error reading dismissed promotions from localStorage:', error);
      return [];
    }
  }

  /**
   * Save dismissed promotions to localStorage
   */
  private saveDismissedPromotions(dismissedPromotions: DismissedPromotion[]): void {
    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(dismissedPromotions));
    } catch (error) {
      console.error('Error saving dismissed promotions to localStorage:', error);
    }
  }

  /**
   * Remove a specific promotion from dismissed list
   */
  private removeDismissedPromotion(promotionId: string): void {
    const dismissedPromotions = this.getDismissedPromotions();
    const filtered = dismissedPromotions.filter(p => p.promotionId !== promotionId);
    this.saveDismissedPromotions(filtered);
  }

  /**
   * Clear all dismissed promotions (useful for testing or user preference reset)
   */
  clearAll(): void {
    try {
      localStorage.removeItem(this.STORAGE_KEY);
    } catch (error) {
      console.error('Error clearing dismissed promotions from localStorage:', error);
    }
  }
}
