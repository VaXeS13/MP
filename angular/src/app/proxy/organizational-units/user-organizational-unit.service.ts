import type { AssignUserDto, JoinUnitResultDto, MyUnitDto, SwitchUnitDto, UpdateUserRoleDto, UserInUnitDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class UserOrganizationalUnitService {
  apiName = 'Default';
  

  assignUserToUnit = (unitId: string, input: AssignUserDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, UserInUnitDto>({
      method: 'POST',
      url: `/api/app/user-organizational-unit/assign-user-to-unit/${unitId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  getMyUnits = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, MyUnitDto[]>({
      method: 'GET',
      url: '/api/app/user-organizational-unit/my-units',
    },
    { apiName: this.apiName,...config });
  

  getUsersInUnit = (unitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, UserInUnitDto[]>({
      method: 'GET',
      url: `/api/app/user-organizational-unit/users-in-unit/${unitId}`,
    },
    { apiName: this.apiName,...config });
  

  joinUnitWithCode = (code: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, JoinUnitResultDto>({
      method: 'POST',
      url: '/api/app/user-organizational-unit/join-unit-with-code',
      params: { code },
    },
    { apiName: this.apiName,...config });
  

  removeUserFromUnit = (unitId: string, userId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: '/api/app/user-organizational-unit/user-from-unit',
      params: { unitId, userId },
    },
    { apiName: this.apiName,...config });
  

  switchUnit = (unitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, SwitchUnitDto>({
      method: 'POST',
      url: `/api/app/user-organizational-unit/switch-unit/${unitId}`,
    },
    { apiName: this.apiName,...config });
  

  updateUserRole = (unitId: string, userId: string, input: UpdateUserRoleDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, UserInUnitDto>({
      method: 'PUT',
      url: '/api/app/user-organizational-unit/user-role',
      params: { unitId, userId },
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
