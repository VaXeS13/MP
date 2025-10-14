import { mapEnumToOptions } from '@abp/ng.core';

export enum CalendarDateStatus {
  Available = 0,
  Reserved = 1,
  Occupied = 2,
  Unavailable = 3,
  PastDate = 4,
  Historical = 5,
}

export const calendarDateStatusOptions = mapEnumToOptions(CalendarDateStatus);
