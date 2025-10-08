export interface BoothTypeDto {
  id: string;
  name: string;
  description: string;
  commissionPercentage: number;
  isActive: boolean;
  creationTime: Date;
  lastModificationTime?: Date;
}

export interface CreateBoothTypeDto {
  name: string;
  description: string;
  commissionPercentage: number;
}

export interface UpdateBoothTypeDto {
  name: string;
  description: string;
  commissionPercentage: number;
  isActive: boolean;
}

export interface GetBoothTypeListDto {
  filter?: string;
  skipCount: number;
  maxResultCount: number;
  sorting?: string;
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
}