export interface BoothDto {
  id: string;
  number: string;
  status: BoothStatus;
  statusDisplayName: string;
  pricePerDay: number;
  currency: Currency;
  currencyDisplayName: string;
  creationTime: Date;
  lastModificationTime?: Date;
  rentalStartDate?: Date;
  rentalEndDate?: Date;
}

export interface BoothListDto {
  id: string;
  number: string;
  status: BoothStatus;
  statusDisplayName: string;
  pricePerDay: number;
  currency: Currency;
  currencyDisplayName: string;
  creationTime: Date;
  rentalStartDate?: Date;
  rentalEndDate?: Date;
}

export interface CreateBoothDto {
  number: string;
  pricePerDay: number;
  currency: Currency;
}

export interface UpdateBoothDto {
  number: string;
  pricePerDay: number;
  currency: Currency;
  status: BoothStatus;
}


export enum BoothStatus {
  Available = 1,
  Reserved = 2,
  Rented = 3,
  Maintenance = 4
}

export enum Currency {
  PLN = 1,
  EUR = 2,
  USD = 3,
  GBP = 4,
  CZK = 5
}

export interface GetBoothListDto {
  filter?: string;
  status?: BoothStatus;
  skipCount: number;
  maxResultCount: number;
  sorting?: string;
}