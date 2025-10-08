import { mapEnumToOptions } from '@abp/ng.core';

export enum BoothStatus {
  Available = 1,
  Reserved = 2,
  Rented = 3,
  Maintenance = 4,
}

export const boothStatusOptions = mapEnumToOptions(BoothStatus);
