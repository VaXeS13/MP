import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { OrganizationalUnitService as ProxyOrganizationalUnitService } from '../proxy/organizational-units/organizational-unit.service';
import {
  OrganizationalUnitDto,
  CreateUpdateOrganizationalUnitDto,
  OrganizationalUnitSettingsDto,
  UpdateUnitSettingsDto,
} from '../proxy/organizational-units/dtos/models';

@Injectable({
  providedIn: 'root',
})
export class OrganizationalUnitService {
  constructor(private proxyService: ProxyOrganizationalUnitService) {}

  /**
   * Get all organizational units for the current user
   */
  getUnits(): Observable<OrganizationalUnitDto[]> {
    return this.proxyService.getList();
  }

  /**
   * Get a specific organizational unit by ID
   */
  getUnit(id: string): Observable<OrganizationalUnitDto> {
    return this.proxyService.get(id);
  }

  /**
   * Create a new organizational unit
   */
  createUnit(input: CreateUpdateOrganizationalUnitDto): Observable<OrganizationalUnitDto> {
    return this.proxyService.create(input);
  }

  /**
   * Update an existing organizational unit
   */
  updateUnit(id: string, input: CreateUpdateOrganizationalUnitDto): Observable<OrganizationalUnitDto> {
    return this.proxyService.update(id, input);
  }

  /**
   * Delete an organizational unit
   */
  deleteUnit(id: string): Observable<void> {
    return this.proxyService.delete(id);
  }

  /**
   * Get organizational unit settings
   */
  getUnitSettings(unitId: string): Observable<OrganizationalUnitSettingsDto> {
    return this.proxyService.getSettings(unitId);
  }

  /**
   * Update organizational unit settings
   */
  updateUnitSettings(
    unitId: string,
    input: UpdateUnitSettingsDto
  ): Observable<OrganizationalUnitSettingsDto> {
    return this.proxyService.updateSettings(unitId, input);
  }

  /**
   * Switch to a different organizational unit
   */
  switchUnit(unitId: string): Observable<any> {
    return this.proxyService.switchUnit(unitId);
  }
}
