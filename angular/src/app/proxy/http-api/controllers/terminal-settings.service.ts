import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { CreateTerminalSettingsDto, TerminalProviderInfoDto, TerminalSettingsDto, UpdateTerminalSettingsDto } from '../../application/contracts/terminals/models';

@Injectable({
  providedIn: 'root',
})
export class TerminalSettingsService {
  apiName = 'Default';
  

  create = (input: CreateTerminalSettingsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TerminalSettingsDto>({
      method: 'POST',
      url: '/api/app/terminal-settings',
      params: { providerId: input.providerId, isEnabled: input.isEnabled, configurationJson: input.configurationJson, currency: input.currency, region: input.region, isSandbox: input.isSandbox },
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/terminal-settings/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getAvailableProviders = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, TerminalProviderInfoDto[]>({
      method: 'GET',
      url: '/api/app/terminal-settings/providers',
    },
    { apiName: this.apiName,...config });
  

  getCurrentTenantSettings = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, TerminalSettingsDto>({
      method: 'GET',
      url: '/api/app/terminal-settings/current',
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateTerminalSettingsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TerminalSettingsDto>({
      method: 'PUT',
      url: `/api/app/terminal-settings/${id}`,
      params: { providerId: input.providerId, isEnabled: input.isEnabled, configurationJson: input.configurationJson, currency: input.currency, region: input.region, isSandbox: input.isSandbox },
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
