import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  SystemContextBoundaryDto,
  SystemHealthStatusDto,
} from '../contracts/api.models';

@Injectable({ providedIn: 'root' })
export class ApiClientService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getSystemStatus(): Observable<ApiResponse<SystemHealthStatusDto>> {
    return this.http.get<ApiResponse<SystemHealthStatusDto>>(`${this.baseUrl}/v1/system/status`);
  }

  getContextBoundary(): Observable<ApiResponse<SystemContextBoundaryDto>> {
    return this.http.get<ApiResponse<SystemContextBoundaryDto>>(`${this.baseUrl}/v1/system/context`);
  }
}

