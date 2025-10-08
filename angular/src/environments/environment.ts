import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';
const host = window.location.host; // np. cto.localhost:4200 lub localhost:4200

function getSubdomain(host: string): string | null {
  // usuń port
  const hostname = host.split(':')[0]; // cto.localhost lub localhost
  if (hostname === 'localhost') return null; // brak subdomeny → tenant domyślny
  
  // sprawdzamy format subdomain.localhost
  if (hostname.endsWith('.localhost')) {
    return hostname.split('.')[0]; // 'cto' lub 'kiss' - lowercase
  }
  return null;
}

const subdomain = getSubdomain(host);
// Client ID: tenant zawsze UPPERCASE, subdomena może być lowercase
const clientId = subdomain ? `MP_App_${subdomain.toUpperCase()}` : 'MP_App'; 

const oAuthConfig = {
  issuer: 'https://localhost:44377/',
  redirectUri: baseUrl,
  clientId: clientId,
  responseType: 'code',
  scope: 'offline_access MP',
  requireHttps: false
};

// Funkcja do dynamicznego określania konfiguracji na podstawie subdomeny
function getEnvironmentConfig(): Partial<Environment> {
  const config: Partial<Environment> = {
    production: false,
    application: {
      baseUrl,
      name: 'MP',
      logoUrl: '',
    },
    oAuthConfig: {
      ...oAuthConfig,
      redirectUri: `${window.location.protocol}//${window.location.host}`,
    },
    apis: {
      default: {
        url: 'https://localhost:44377',
        rootNamespace: 'MP',
      },
    },
  };

  // Dodaj informację o tenancie jeśli subdomena istnieje
  // TenantName powinien być UPPERCASE jak w bazie danych
  if (subdomain) {
    config.tenantName = subdomain.toUpperCase();
  }

  return config;
}

export const environment = getEnvironmentConfig() as Environment;