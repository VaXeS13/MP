import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { AssignUserDto, JoinUnitDto, JoinUnitResultDto, MyUnitDto, UpdateUserRoleDto, UserInUnitDto } from '../organizational-units/dtos/models';

@Injectable({
  providedIn: 'root',
})
export class UserOrganizationalUnitsService {
  apiName = 'Default';
  

  assignUserToUnit = (unitId: string, input: AssignUserDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, UserInUnitDto>({
      method: 'POST',
      url: `/api/app/organizational-units/${unitId}/users`,
      params: { userId: input.userId, roleId: input.roleId },
    },
    { apiName: this.apiName,...config });
  

  getMyUnits = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, MyUnitDto[]>({
      method: 'GET',
      url: '/api/app/organizational-units/my-units',
    },
    { apiName: this.apiName,...config });
  

  getUsersInUnit = (unitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, UserInUnitDto[]>({
      method: 'GET',
      url: `/api/app/organizational-units/${unitId}/users`,
    },
    { apiName: this.apiName,...config });
  

  joinUnitWithCode = (input: JoinUnitDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, JoinUnitResultDto>({
      method: 'POST',
      url: '/api/app/organizational-units/join',
      params: { code: input.code },
    },
    { apiName: this.apiName,...config });
  

  removeUserFromUnit = (unitId: string, userId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/organizational-units/${unitId}/users/${userId}`,
    },
    { apiName: this.apiName,...config });
  

  updateUserRole = (unitId: string, userId: string, input: UpdateUserRoleDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, UserInUnitDto>({
      method: 'PUT',
      url: `/api/app/organizational-units/${unitId}/users/${userId}/role`,
      params: { roleId: input.roleId },
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
