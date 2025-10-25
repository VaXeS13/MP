
export interface AssignUserDto {
  userId?: string;
  roleId?: string;
}

export interface CreateRegistrationCodeDto {
  roleId?: string;
  maxUsageCount?: number;
  expirationDays?: number;
}

export interface CreateUpdateOrganizationalUnitDto {
  name: string;
  code: string;
  address?: string;
  city?: string;
  postalCode?: string;
  email?: string;
  phone?: string;
  isActive: boolean;
}

export interface CurrentUnitDto {
  unitId?: string;
  unitName?: string;
  unitCode?: string;
  currency?: string;
  userRole?: string;
  settings: OrganizationalUnitSettingsDto;
}

export interface GenerateRegistrationCodeRequestDto {
  organizationalUnitId?: string;
  createDto: CreateRegistrationCodeDto;
}

export interface JoinUnitDto {
  code: string;
}

export interface JoinUnitResultDto {
  unitId?: string;
  unitName?: string;
  requiresRegistration: boolean;
}

export interface MyUnitDto {
  unitId?: string;
  unitName?: string;
  unitCode?: string;
  role?: string;
  currency?: string;
}

export interface OrganizationalUnitDto {
  id?: string;
  name?: string;
  code?: string;
  address?: string;
  city?: string;
  postalCode?: string;
  email?: string;
  phone?: string;
  isActive: boolean;
  creationTime?: string;
  lastModificationTime?: string;
  settings: OrganizationalUnitSettingsDto;
}

export interface OrganizationalUnitSettingsDto {
  id?: string;
  organizationalUnitId?: string;
  currency?: string;
  enabledPaymentProviders: Record<string, boolean>;
  defaultPaymentProvider?: string;
  logoUrl?: string;
  bannerText?: string;
  isMainUnit: boolean;
  creationTime?: string;
  lastModificationTime?: string;
}

export interface RegistrationCodeDto {
  id?: string;
  organizationalUnitId?: string;
  code?: string;
  roleId?: string;
  expiresAt?: string;
  maxUsageCount?: number;
  usageCount: number;
  lastUsedAt?: string;
  isActive: boolean;
  isExpired: boolean;
  isUsageLimitReached: boolean;
}

export interface SwitchUnitDto {
  unitId?: string;
  unitName?: string;
  cookieSet: boolean;
}

export interface UpdateUnitSettingsDto {
  currency: string;
  enabledPaymentProviders: Record<string, boolean>;
  defaultPaymentProvider?: string;
  logoUrl?: string;
  bannerText?: string;
}

export interface UpdateUserRoleDto {
  roleId?: string;
}

export interface UserInUnitDto {
  userId?: string;
  userName?: string;
  email?: string;
  role?: string;
  assignedAt?: string;
  isActive: boolean;
}

export interface ValidateCodeResultDto {
  isValid: boolean;
  unitId?: string;
  unitName?: string;
  reason?: string;
}
