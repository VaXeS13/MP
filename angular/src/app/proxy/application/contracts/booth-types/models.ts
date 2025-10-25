import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface BoothTypeDto extends FullAuditedEntityDto<string> {
  name?: string;
  description?: string;
  commissionPercentage: number;
  isActive: boolean;
}

export interface CreateBoothTypeDto {
  organizationalUnitId: string;
  name: string;
  description: string;
  commissionPercentage: number;
}

export interface UpdateBoothTypeDto {
  name: string;
  description: string;
  commissionPercentage: number;
}
