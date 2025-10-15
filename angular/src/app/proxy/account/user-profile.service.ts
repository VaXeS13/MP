import type { UserProfileDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class UserProfileService {
  apiName = 'Default';
  

  get = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, UserProfileDto>({
      method: 'GET',
      url: '/api/app/user-profile',
    },
    { apiName: this.apiName,...config });
  

  update = (input: UserProfileDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, UserProfileDto>({
      method: 'PUT',
      url: '/api/app/user-profile',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
