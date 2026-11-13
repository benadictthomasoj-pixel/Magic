export const MAGIC_HTTP_HEADERS = {
  branchId: 'X-Branch-Id',
  terminalId: 'X-Terminal-Id',
  idempotencyKey: 'Idempotency-Key',
  correlationId: 'X-Correlation-Id',
  supervisorAuthToken: 'X-Supervisor-Auth-Token',
} as const;

export interface PaginationMeta {
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}

export interface ApiResponse<T, TMeta = PaginationMeta | Record<string, unknown>> {
  readonly success: boolean;
  readonly data: T;
  readonly meta?: TMeta | null;
  readonly correlationId: string;
  readonly timestampUtc: string;
}

export interface FieldValidationError {
  readonly field: string;
  readonly message: string;
}

export interface ApiErrorDetails {
  readonly type: string;
  readonly title: string;
  readonly status: number;
  readonly detail: string;
  readonly errorCode: string;
  readonly invalidFields?: readonly FieldValidationError[] | null;
}

export interface ApiErrorResponse {
  readonly success: boolean;
  readonly error: ApiErrorDetails;
  readonly correlationId: string;
  readonly timestampUtc: string;
}

export interface ComponentHealthDto {
  readonly name: string;
  readonly status: string;
  readonly detail?: string | null;
}

export interface SystemHealthStatusDto {
  readonly status: string;
  readonly serviceName: string;
  readonly version: string;
  readonly environment: string;
  readonly deploymentMode: string;
  readonly databaseConnected: boolean;
  readonly networkPolicy: string;
  readonly authenticationStatus: string;
  readonly timestampUtc: string;
  readonly components: readonly ComponentHealthDto[];
}

export interface SystemContextBoundaryDto {
  readonly branchId: string | null;
  readonly terminalId: string | null;
  readonly authenticationConfigured: boolean;
  readonly openDecisions: readonly string[];
}

export interface DesktopBridgeMessage {
  readonly messageType: string;
  readonly correlationId: string;
  readonly payloadJson?: string | null;
  readonly timestampUtc?: string;
}

export interface DesktopHostCapabilitiesDto {
  readonly hostPlatform: string;
  readonly supportsEscPosRawPrinting: boolean;
  readonly supportsCashDrawerPulse: boolean;
  readonly supportsSerialWeighScale: boolean;
  readonly shellVersion: string;
}

