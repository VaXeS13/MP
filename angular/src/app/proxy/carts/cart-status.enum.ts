import { mapEnumToOptions } from '@abp/ng.core';

export enum CartStatus {
  Active = 0,
  CheckedOut = 1,
  Abandoned = 2,
}

export const cartStatusOptions = mapEnumToOptions(CartStatus);
