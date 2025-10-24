
export interface AssignUserDto {
  userId?: string;
  roleId?: string;
}

export interface CreateRegistrationCodeDto {
  roleId?: string;
  maxUsageCount?: number;
  expirationDays?: number;
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
