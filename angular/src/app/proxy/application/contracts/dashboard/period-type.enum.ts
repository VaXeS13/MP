import { mapEnumToOptions } from '@abp/ng.core';

export enum PeriodType {
  Day = 0,
  Week = 1,
  Month = 2,
  Quarter = 3,
  Year = 4,
  Custom = 5,
}

export const periodTypeOptions = mapEnumToOptions(PeriodType);
