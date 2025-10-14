import type { EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { BoothStatus } from '../domain/booths/booth-status.enum';

export interface BoothDto extends FullAuditedEntityDto<string> {
  number?: string;
  status?: BoothStatus;
  statusDisplayName?: string;
  pricePerDay: number;
  rentalStartDate?: string;
  rentalEndDate?: string;
  currentRentalId?: string;
  currentRentalUserName?: string;
  currentRentalUserEmail?: string;
  currentRentalStartDate?: string;
  currentRentalEndDate?: string;
}

export interface BoothListDto extends EntityDto<string> {
  number?: string;
  status?: BoothStatus;
  statusDisplayName?: string;
  pricePerDay: number;
  creationTime?: string;
  rentalStartDate?: string;
  rentalEndDate?: string;
  currentRentalId?: string;
  currentRentalUserName?: string;
  currentRentalUserEmail?: string;
  currentRentalStartDate?: string;
  currentRentalEndDate?: string;
}

export interface BoothSettingsDto {
  minimumGapDays: number;
}

export interface CreateBoothDto {
  number: string;
  pricePerDay: number;
}

export interface CreateManualReservationDto {
  boothId: string;
  userId: string;
  startDate: string;
  endDate: string;
  targetStatus: BoothStatus;
}

export interface GetBoothListDto extends PagedAndSortedResultRequestDto {
  filter?: string;
  status?: BoothStatus;
}

export interface UpdateBoothDto {
  number: string;
  pricePerDay: number;
  status?: BoothStatus;
}
