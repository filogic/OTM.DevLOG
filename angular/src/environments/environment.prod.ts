import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

export const environment = {
  production: true,
  application: {
    baseUrl,
    name: 'DevLOG',
    logoUrl: '',
  },
  oAuthConfig: {
    issuer: 'https://localhost:44369/',
    redirectUri: baseUrl,
    clientId: 'DevLOG_App',
    responseType: 'code',
    scope: 'offline_access DevLOG',
    requireHttps: true
  },
  apis: {
    default: {
      url: 'https://localhost:44336',
      rootNamespace: 'OTM.DevLOG',
    },
  },
} as Environment;
