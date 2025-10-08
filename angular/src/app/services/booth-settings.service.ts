import { Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface BoothSettingsDto {
  minimumGapDays: number;
}

@Injectable({
  providedIn: 'root',
})
export class BoothSettingsService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  get = (): Observable<BoothSettingsDto> =>
    this.restService.request<void, BoothSettingsDto>(
      {
        method: 'GET',
        url: '/api/app/booth-settings',
      },
      { apiName: this.apiName }
    );

  update = (input: BoothSettingsDto): Observable<void> =>
    this.restService.request<BoothSettingsDto, void>(
      {
        method: 'PUT',
        url: '/api/app/booth-settings',
        body: input,
      },
      { apiName: this.apiName }
    );
}
