import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { GenerateRegistrationCodeRequestDto, JoinUnitDto, JoinUnitResultDto, RegistrationCodeDto, ValidateCodeResultDto } from '../organizational-units/dtos/models';

@Injectable({
  providedIn: 'root',
})
export class RegistrationCodesService {
  apiName = 'Default';
  

  deactivateCode = (codeId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/registration-codes/${codeId}`,
    },
    { apiName: this.apiName,...config });
  

  generateCode = (input: GenerateRegistrationCodeRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RegistrationCodeDto>({
      method: 'POST',
      url: '/api/app/registration-codes/generate',
      params: { organizationalUnitId: input.organizationalUnitId, ["CreateDto.RoleId"]: input.createDto.roleId, ["CreateDto.MaxUsageCount"]: input.createDto.maxUsageCount, ["CreateDto.ExpirationDays"]: input.createDto.expirationDays },
    },
    { apiName: this.apiName,...config });
  

  joinUnitWithCode = (input: JoinUnitDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, JoinUnitResultDto>({
      method: 'POST',
      url: '/api/app/registration-codes/join',
      params: { code: input.code },
    },
    { apiName: this.apiName,...config });
  

  listCodes = (unitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RegistrationCodeDto[]>({
      method: 'GET',
      url: `/api/app/registration-codes/by-unit/${unitId}`,
    },
    { apiName: this.apiName,...config });
  

  validateCode = (code: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ValidateCodeResultDto>({
      method: 'GET',
      url: '/api/app/registration-codes/validate',
      params: { code },
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
