export interface MagicEnvironmentConfig {
  readonly production: boolean;
  readonly apiBaseUrl: string;
  readonly connectivityPollIntervalMs: number;
}

