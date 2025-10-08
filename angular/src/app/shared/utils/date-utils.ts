/**
 * Date utility functions for handling timezone-safe date operations
 */

/**
 * Converts a Date object to YYYY-MM-DD format using local timezone
 * This prevents timezone conversion issues when sending dates to the server
 *
 * @param date Date object to format
 * @returns Date string in YYYY-MM-DD format (e.g., "2025-09-30")
 *
 * @example
 * const date = new Date(2025, 8, 30); // September 30, 2025
 * formatDateToLocal(date); // Returns "2025-09-30" regardless of timezone
 */
export function formatDateToLocal(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/**
 * Parses a date string in YYYY-MM-DD format to a Date object
 * The date is created at midnight in the local timezone
 *
 * @param dateStr Date string in YYYY-MM-DD format
 * @returns Date object at midnight local time
 *
 * @example
 * const date = parseDateFromLocal("2025-09-30");
 * // Returns Date object for September 30, 2025 at 00:00:00 local time
 */
export function parseDateFromLocal(dateStr: string): Date {
  const [year, month, day] = dateStr.split('-').map(Number);
  return new Date(year, month - 1, day);
}