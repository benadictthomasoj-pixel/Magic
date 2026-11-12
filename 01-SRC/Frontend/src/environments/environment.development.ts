import { MagicEnvironmentConfig } from './environment.model';

export const environment: MagicEnvironmentConfig = {
  production: false,
  apiBaseUrl: 'http://127.0.0.1:5000/api',
  connectivityPollIntervalMs: 15_000,
};
