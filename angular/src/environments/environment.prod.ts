import { Environment } from '@abp/ng.core';

const host = window.location.host; // e.g., cto.mp.com or mp.com
const protocol = window.location.protocol;

function getSubdomain(host: string): string | null {
  const hostname = host.split(':')[0]; // Remove port if present

  // Check for production domain format
  if (hostname.endsWith('.mp.com') && hostname !== 'mp.com' && hostname !== 'www.mp.com') {
    return hostname.split('.')[0]; // Extract subdomain
  }

  return null; // No subdomain (main domain)
}

const subdomain = getSubdomain(host);
const baseUrl = `${protocol}//${host}`;
const apiUrl = 'https://api.mp.com'; // Your production API URL

// Dynamic client ID based on tenant
const clientId = subdomain ? `MP_App_${subdomain.toUpperCase()}` : 'MP_App';

const oAuthConfig = {
  issuer: apiUrl,
  redirectUri: baseUrl,
  clientId: clientId,
  responseType: 'code',
  scope: 'offline_access MP',
  requireHttps: true,
};

function getEnvironmentConfig(): Partial<Environment> {
  const config: Partial<Environment> = {
    production: true,
    application: {
      baseUrl,
      name: subdomain ? `MP - ${subdomain.toUpperCase()}` : 'MP Platform',
      logoUrl: '',
    },
    oAuthConfig: {
      ...oAuthConfig,
      redirectUri: baseUrl,
    },
    apis: {
      default: {
        url: apiUrl,
        rootNamespace: 'MP',
      },
      AbpAccountPublic: {
        url: apiUrl,
        rootNamespace: 'AbpAccountPublic',
      },
    },
    remoteEnv: {
      url: '/getEnvConfig',
      mergeStrategy: 'deepmerge'
    }
  };

  // Add tenant information if subdomain exists
  if (subdomain) {
    config.tenantName = subdomain.toUpperCase();
  }

  return config;
}

export const environment = getEnvironmentConfig() as Environment;
