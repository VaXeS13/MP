import { mapEnumToOptions } from '@abp/ng.core';

export enum PaymentMethodType {
  Cash = 0,
  Card = 1,
}

export const paymentMethodTypeOptions = mapEnumToOptions(PaymentMethodType);
