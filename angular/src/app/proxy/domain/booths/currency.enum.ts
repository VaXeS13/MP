import { mapEnumToOptions } from '@abp/ng.core';

export enum Currency {
  PLN = 1,
  EUR = 2,
  USD = 3,
  GBP = 4,
  CZK = 5,
}

export const currencyOptions = mapEnumToOptions(Currency);
