import { mapEnumToOptions } from '@abp/ng.core';

export enum ExportFormat {
  Excel = 0,
  Pdf = 1,
  Csv = 2,
}

export const exportFormatOptions = mapEnumToOptions(ExportFormat);
