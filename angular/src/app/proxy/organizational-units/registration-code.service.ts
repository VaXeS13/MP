import type { CreateRegistrationCodeDto, JoinUnitResultDto, RegistrationCodeDto, ValidateCodeResultDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class RegistrationCodeService {
  apiName = 'Default';
  

  deactivateCode = (codeId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/registration-code/deactivate-code/${codeId}`,
    },
    { apiName: this.apiName,...config });
  

  generateCode = (organizationalUnitId: string, input: CreateRegistrationCodeDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RegistrationCodeDto>({
      method: 'POST',
      url: `/api/app/registration-code/generate-code/${organizationalUnitId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  joinUnitWithCode = (code: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, JoinUnitResultDto>({
      method: 'POST',
      url: '/api/app/registration-code/join-unit-with-code',
      params: { code },
    },
    { apiName: this.apiName,...config });
  

  listCodes = (organizationalUnitId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RegistrationCodeDto[]>({
      method: 'POST',
      url: `/api/app/registration-code/list-codes/${organizationalUnitId}`,
    },
    { apiName: this.apiName,...config });
  

  validateCode = (code: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ValidateCodeResultDto>({
      method: 'POST',
      url: '/api/app/registration-code/validate-code',
      params: { code },
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
