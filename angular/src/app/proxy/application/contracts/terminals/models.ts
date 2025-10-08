
export interface CreateTerminalSettingsDto {
  providerId: string;
  isEnabled: boolean;
  configurationJson?: string;
  currency: string;
  region?: string;
  isSandbox: boolean;
}

export interface TerminalProviderInfoDto {
  providerId?: string;
  displayName?: string;
  description?: string;
}

export interface TerminalSettingsDto {
  id?: string;
  tenantId?: string;
  providerId?: string;
  isEnabled: boolean;
  configurationJson?: string;
  currency?: string;
  region?: string;
  isSandbox: boolean;
}

export interface UpdateTerminalSettingsDto {
  providerId: string;
  isEnabled: boolean;
  configurationJson?: string;
  currency: string;
  region?: string;
  isSandbox: boolean;
}
