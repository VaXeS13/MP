import { Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';
import { PagedResultDto } from '@abp/ng.core';
import { BoothDto, BoothListDto, CreateBoothDto, UpdateBoothDto, GetBoothListDto, BoothStatus } from '../shared/models/booth.model';

@Injectable({
  providedIn: 'root'
})
export class BoothService {
  constructor(private rest: RestService) {
      console.log('BoothService działa');
  }

  getList(input: GetBoothListDto): Observable<PagedResultDto<BoothListDto>> {
    return this.rest.request<any, PagedResultDto<BoothListDto>>({
      method: 'GET',
      url: '/api/app/booth',
      params: input
    });
  }

  get(id: string): Observable<BoothDto> {
    return this.rest.request<any, BoothDto>({
      method: 'GET',
      url: `/api/app/booth/${id}`
    });
  }

  create(input: CreateBoothDto): Observable<BoothDto> {
    return this.rest.request<CreateBoothDto, BoothDto>({
      method: 'POST',
      url: '/api/app/booth',
      body: input
    });
  }

  update(id: string, input: UpdateBoothDto): Observable<BoothDto> {
    return this.rest.request<UpdateBoothDto, BoothDto>({
      method: 'PUT',
      url: `/api/app/booth/${id}`,
      body: input
    });
  }

  delete(id: string): Observable<void> {
    return this.rest.request<any, void>({
      method: 'DELETE',
      url: `/api/app/booth/${id}`
    });
  }

  getAvailableBooths(): Observable<BoothDto[]> {
    return this.rest.request<any, BoothDto[]>({
      method: 'GET',
      url: '/api/app/booth/available'
    });
  }

  changeStatus(id: string, newStatus: BoothStatus): Observable<BoothDto> {
    return this.rest.request<any, BoothDto>({
      method: 'PUT',
      url: `/api/app/booth/${id}/change-status?newStatus=${newStatus}`
    });
  }

  getMyBooths(input: GetBoothListDto): Observable<PagedResultDto<BoothListDto>> {
    return this.rest.request<any, PagedResultDto<BoothListDto>>({
      method: 'GET',
      url: '/api/app/booth/my-booths',
      params: input
    });
  }

  createManualReservation(input: any): Observable<BoothDto> {
    return this.rest.request<any, BoothDto>({
      method: 'POST',
      url: '/api/app/booth/manual-reservation',
      body: input
    });
  }
}