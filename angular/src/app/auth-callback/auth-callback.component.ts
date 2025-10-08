import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';
import { TenantService } from '../shared/services/tenant.service';

@Component({
  standalone: false,
  selector: 'app-auth-callback',
  template: `
    <div style="display: flex; justify-content: center; align-items: center; height: 100vh;">
      <div>
        <h3>Processing authentication...</h3>
        <p>Please wait while we redirect you to your tenant.</p>
      </div>
    </div>
  `
})
export class AuthCallbackComponent implements OnInit {

  constructor(
    private oauthService: OAuthService,
    private router: Router,
    private tenantService: TenantService
  ) {}

  async ngOnInit(): Promise<void> {
    try {
      console.log('Processing OAuth callback...');

      // Process the OAuth callback
      await this.oauthService.tryLogin();

      // Check if user is now logged in
      if (this.oauthService.hasValidIdToken() && this.oauthService.hasValidAccessToken()) {
        console.log('Authentication successful');

        // Get current tenant info
        const tenant = this.tenantService.getCurrentTenant();
        console.log('Current tenant context:', tenant);

        // Redirect to appropriate tenant URL
        if (tenant && !tenant.isHost) {
          // For tenant subdomains, redirect to their subdomain
          const targetUrl = `${window.location.protocol}//${tenant.name.toLowerCase()}.localhost:4200`;
          console.log('Redirecting to tenant URL:', targetUrl);
          window.location.href = targetUrl;
        } else {
          // For host tenant, redirect to main app
          console.log('Redirecting to main app');
          this.router.navigate(['/']);
        }
      } else {
        console.error('Authentication failed');
        this.router.navigate(['/account/login']);
      }
    } catch (error) {
      console.error('Error processing authentication callback:', error);
      this.router.navigate(['/account/login']);
    }
  }
}