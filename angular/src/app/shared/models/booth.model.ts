export interface BoothDto {
  id: string;
  number: string;
  status: BoothStatus;
  statusDisplayName: string;
  pricePerDay: number;
  creationTime: Date;
  lastModificationTime?: Date;
  rentalStartDate?: Date;
  rentalEndDate?: Date;
  // Current active rental information
  currentRentalId?: string;
  currentRentalUserName?: string;
  currentRentalUserEmail?: string;
  currentRentalStartDate?: Date;
  currentRentalEndDate?: Date;
}

export interface BoothListDto {
  id: string;
  number: string;
  status: BoothStatus;
  statusDisplayName: string;
  pricePerDay: number;
  creationTime: Date;
  rentalStartDate?: Date;
  rentalEndDate?: Date;
  // Current active rental information
  currentRentalId?: string;
  currentRentalUserName?: string;
  currentRentalUserEmail?: string;
  currentRentalStartDate?: Date;
  currentRentalEndDate?: Date;
}

export interface CreateBoothDto {
  number: string;
  pricePerDay: number;
}

export interface UpdateBoothDto {
  number: string;
  pricePerDay: number;
  status: BoothStatus;
}


export enum BoothStatus {
  Available = 1,
  Reserved = 2,
  Rented = 3,
  Maintenance = 4
}

export interface GetBoothListDto {
  filter?: string;
  status?: BoothStatus;
  skipCount: number;
  maxResultCount: number;
  sorting?: string;
}