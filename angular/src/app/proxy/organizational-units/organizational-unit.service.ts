import type { CreateUpdateOrganizationalUnitDto, CurrentUnitDto, OrganizationalUnitDto, OrganizationalUnitSettingsDto, SwitchUnitDto, UpdateUnitSettingsDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class OrganizationalUnitService {
  apiName = 'Default';
  

  create = (input: CreateUpdateOrganizationalUnitDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitDto>({
      method: 'POST',
      url: '/api/app/organizational-unit',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/organizational-unit/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitDto>({
      method: 'GET',
      url: `/api/app/organizational-unit/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getCurrentUnit = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, CurrentUnitDto>({
      method: 'GET',
      url: '/api/app/organizational-unit/current-unit',
    },
    { apiName: this.apiName,...config });
  

  getList = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitDto[]>({
      method: 'GET',
      url: '/api/app/organizational-unit',
    },
    { apiName: this.apiName,...config });
  

  getSettings = (unitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitSettingsDto>({
      method: 'GET',
      url: `/api/app/organizational-unit/settings/${unitId}`,
    },
    { apiName: this.apiName,...config });
  

  switchUnit = (unitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, SwitchUnitDto>({
      method: 'POST',
      url: `/api/app/organizational-unit/switch-unit/${unitId}`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateOrganizationalUnitDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitDto>({
      method: 'PUT',
      url: `/api/app/organizational-unit/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  updateSettings = (unitId: string, input: UpdateUnitSettingsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, OrganizationalUnitSettingsDto>({
      method: 'PUT',
      url: `/api/app/organizational-unit/settings/${unitId}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
