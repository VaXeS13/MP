import type { BoothDto, BoothListDto, CreateBoothDto, CreateManualReservationDto, GetBoothListDto, UpdateBoothDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { BoothStatus } from '../domain/booths/booth-status.enum';

@Injectable({
  providedIn: 'root',
})
export class BoothService {
  apiName = 'Default';
  

  changeStatus = (id: string, newStatus: BoothStatus, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothDto>({
      method: 'POST',
      url: `/api/app/booth/${id}/change-status`,
      params: { newStatus },
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateBoothDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothDto>({
      method: 'POST',
      url: '/api/app/booth',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  createManualReservation = (input: CreateManualReservationDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothDto>({
      method: 'POST',
      url: '/api/app/booth/manual-reservation',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/booth/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothDto>({
      method: 'GET',
      url: `/api/app/booth/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getAvailableBooths = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothDto[]>({
      method: 'GET',
      url: '/api/app/booth/available-booths',
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetBoothListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<BoothListDto>>({
      method: 'GET',
      url: '/api/app/booth',
      params: { filter: input.filter, status: input.status, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getMyBooths = (input: GetBoothListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<BoothListDto>>({
      method: 'GET',
      url: '/api/app/booth/my-booths',
      params: { filter: input.filter, status: input.status, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateBoothDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothDto>({
      method: 'PUT',
      url: `/api/app/booth/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
