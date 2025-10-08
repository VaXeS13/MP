import { Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';
import { PagedResultDto } from '@abp/ng.core';
import {
  BoothTypeDto,
  CreateBoothTypeDto,
  UpdateBoothTypeDto,
  GetBoothTypeListDto
} from '../shared/models/booth-type.model';

@Injectable({
  providedIn: 'root'
})
export class BoothTypeService {
  constructor(private rest: RestService) {
    console.log('BoothTypeService initialized');
  }

  getList(input: GetBoothTypeListDto): Observable<PagedResultDto<BoothTypeDto>> {
    return this.rest.request<any, PagedResultDto<BoothTypeDto>>({
      method: 'GET',
      url: '/api/app/booth-types',
      params: input
    });
  }

  get(id: string): Observable<BoothTypeDto> {
    return this.rest.request<any, BoothTypeDto>({
      method: 'GET',
      url: `/api/app/booth-types/${id}`
    });
  }

  create(input: CreateBoothTypeDto): Observable<BoothTypeDto> {
    return this.rest.request<CreateBoothTypeDto, BoothTypeDto>({
      method: 'POST',
      url: '/api/app/booth-types',
      body: input
    });
  }

  update(id: string, input: UpdateBoothTypeDto): Observable<BoothTypeDto> {
    return this.rest.request<UpdateBoothTypeDto, BoothTypeDto>({
      method: 'PUT',
      url: `/api/app/booth-types/${id}`,
      body: input
    });
  }

  delete(id: string): Observable<void> {
    return this.rest.request<any, void>({
      method: 'DELETE',
      url: `/api/app/booth-types/${id}`
    });
  }

  getActiveTypes(): Observable<BoothTypeDto[]> {
    return this.rest.request<any, BoothTypeDto[]>({
      method: 'GET',
      url: '/api/app/booth-types/active'
    });
  }

  activate(id: string): Observable<BoothTypeDto> {
    return this.rest.request<any, BoothTypeDto>({
      method: 'POST',
      url: `/api/app/booth-types/${id}/activate`
    });
  }

  deactivate(id: string): Observable<BoothTypeDto> {
    return this.rest.request<any, BoothTypeDto>({
      method: 'POST',
      url: `/api/app/booth-types/${id}/deactivate`
    });
  }
}