export const MAGIC_HTTP_HEADERS = {
  branchId: 'X-Branch-Id',
  terminalId: 'X-Terminal-Id',
  idempotencyKey: 'X-Idempotency-Key',
  correlationId: 'X-Correlation-Id',
  supervisorAuthToken: 'X-Supervisor-Auth-Token',
} as const;

export interface ApiResponse<T> {
  readonly success: boolean;
  readonly data: T;
  readonly meta?: Record<string, unknown> | null;
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
  readonly details?: string | null;
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
  readonly payloadJson: string;
  readonly correlationId: string;
}

export interface DesktopHostCapabilitiesDto {
  readonly platform: string;
  readonly webView2Available: boolean;
  readonly thermalPrintingSupported: boolean;
  readonly cashDrawerSupported: boolean;
}

