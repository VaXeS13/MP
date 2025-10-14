import { mapEnumToOptions } from '@abp/ng.core';

export enum ExtensionPaymentType {
  Free = 0,
  Cash = 1,
  Terminal = 2,
  Online = 3,
}

export const extensionPaymentTypeOptions = mapEnumToOptions(ExtensionPaymentType);
