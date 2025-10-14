import { mapEnumToOptions } from '@abp/ng.core';

export enum RentalPaymentMethod {
  Online = 0,
  Cash = 1,
  Terminal = 2,
  Free = 3,
}

export const rentalPaymentMethodOptions = mapEnumToOptions(RentalPaymentMethod);
