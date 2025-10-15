import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { UploadFileDto, UploadedFileDto } from '../contracts/files/models';

@Injectable({
  providedIn: 'root',
})
export class UploadedFileService {
  apiName = 'Default';
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/uploaded-file/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, UploadedFileDto>({
      method: 'GET',
      url: `/api/app/uploaded-file/${id}`,
    },
    { apiName: this.apiName,...config });
  

  upload = (input: UploadFileDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, UploadedFileDto>({
      method: 'POST',
      url: '/api/app/uploaded-file/upload',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
