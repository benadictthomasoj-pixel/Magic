import { MagicEnvironmentConfig } from './environment.model';

export const environment: MagicEnvironmentConfig = {
  production: true,
  apiBaseUrl: '/api',
  connectivityPollIntervalMs: 30_000,
};
