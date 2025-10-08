import { mapEnumToOptions } from '@abp/ng.core';

export enum RentalStatus {
  Draft = 0,
  Active = 1,
  Expired = 2,
  Cancelled = 3,
  Extended = 4,
}

export const rentalStatusOptions = mapEnumToOptions(RentalStatus);
