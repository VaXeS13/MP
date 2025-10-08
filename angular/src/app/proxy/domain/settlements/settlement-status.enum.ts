import { mapEnumToOptions } from '@abp/ng.core';

export enum SettlementStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Cancelled = 3,
}

export const settlementStatusOptions = mapEnumToOptions(SettlementStatus);
