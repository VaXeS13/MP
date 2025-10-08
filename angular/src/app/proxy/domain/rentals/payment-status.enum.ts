import { mapEnumToOptions } from '@abp/ng.core';

export enum PaymentStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
  Cancelled = 4,
  Refunded = 5,
}

export const paymentStatusOptions = mapEnumToOptions(PaymentStatus);
