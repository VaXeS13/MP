import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { CreateUpdateOrganizationalUnitDto, CurrentUnitDto, OrganizationalUnitDto, OrganizationalUnitSettingsDto, SwitchUnitDto, UpdateUnitSettingsDto } from '../organizational-units/dtos/models';

@Injectable({
  providedIn: 'root',
})
export class OrganizationalUnitsService {
  apiName = 'Default';
  

  create = (input: CreateUpdateOrganizationalUnitDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitDto>({
      method: 'POST',
      url: '/api/app/organizational-units',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/organizational-units/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitDto>({
      method: 'GET',
      url: `/api/app/organizational-units/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getCurrentUnit = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, CurrentUnitDto>({
      method: 'GET',
      url: '/api/app/organizational-units/current',
    },
    { apiName: this.apiName,...config });
  

  getList = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitDto[]>({
      method: 'GET',
      url: '/api/app/organizational-units',
    },
    { apiName: this.apiName,...config });
  

  getSettings = (unitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitSettingsDto>({
      method: 'GET',
      url: `/api/app/organizational-units/${unitId}/settings`,
    },
    { apiName: this.apiName,...config });
  

  switchUnit = (unitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, SwitchUnitDto>({
      method: 'POST',
      url: '/api/app/organizational-units/switch',
      params: { unitId },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateOrganizationalUnitDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitDto>({
      method: 'PUT',
      url: `/api/app/organizational-units/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  updateSettings = (unitId: string, input: UpdateUnitSettingsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitSettingsDto>({
      method: 'PUT',
      url: `/api/app/organizational-units/${unitId}/settings`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
