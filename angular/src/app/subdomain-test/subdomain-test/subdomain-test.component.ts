import { Component, OnInit } from '@angular/core';
import { SubdomainService, SubdomainInfo } from './../../services/subdomain-info.service';

@Component({
  selector: 'app-subdomain-test',
  standalone:false,
  template: `
    <div class="container mt-4">
      <div class="row">
        <div class="col-md-8">
          <h3>Subdomain Detection Test</h3>
          
          <div class="card mb-3">
            <div class="card-header">
              <h5>Current Environment</h5>
            </div>
            <div class="card-body">
              <p><strong>URL:</strong> {{ currentUrl }}</p>
              <p><strong>Detected Subdomain:</strong> 
                <span class="badge" [ngClass]="currentSubdomain ? 'badge-success' : 'badge-warning'">
                  {{ currentSubdomain || 'None' }}
                </span>
              </p>
              <p><strong>Company:</strong> {{ companyName }}</p>
            </div>
          </div>

          <div class="mb-3">
            <button class="btn btn-primary me-2" (click)="testSubdomainDetection()">
              Test Backend Detection
            </button>
            <button class="btn btn-secondary me-2" (click)="getDebugInfo()">
              Get Debug Info
            </button>
            <button class="btn btn-info" (click)="refreshInfo()">
              Refresh
            </button>
          </div>

          <div *ngIf="subdomainInfo" class="card mb-3">
            <div class="card-header">
              <h5>Backend Response</h5>
            </div>
            <div class="card-body">
              <div *ngIf="subdomainInfo.hasSubdomain" class="alert alert-success">
                <strong>✅ Subdomain Detected:</strong> {{ subdomainInfo.subdomain }}
              </div>
              <div *ngIf="!subdomainInfo.hasSubdomain" class="alert alert-warning">
                <strong>⚠️ No Subdomain Detected</strong>
              </div>
              
              <div *ngIf="subdomainInfo.isValidClient" class="alert alert-info">
                <strong>OAuth Client:</strong> {{ subdomainInfo.clientInfo?.displayName }} ({{ subdomainInfo.clientId }})
              </div>
              
              <div *ngIf="subdomainInfo.isAuthenticated" class="alert alert-success">
                <strong>✅ User Authenticated:</strong> {{ subdomainInfo.userName }}
              </div>
              
              <pre class="mt-2">{{ subdomainInfo | json }}</pre>
            </div>
          </div>

          <div *ngIf="debugInfo" class="card">
            <div class="card-header">
              <h5>Debug Information</h5>
            </div>
            <div class="card-body">
              <pre>{{ debugInfo | json }}</pre>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class SubdomainTestComponent implements OnInit {
  currentUrl: string = '';
  currentSubdomain: string | null = null;
  companyName: string = '';
  subdomainInfo: SubdomainInfo | null = null;
  debugInfo: any = null;

  constructor(private subdomainService: SubdomainService) {}

  ngOnInit(): void {
    this.currentUrl = window.location.href;
    this.currentSubdomain = this.subdomainService.getCurrentSubdomain();
    this.companyName = this.subdomainService.getCompanyName();

    // Subscribe to subdomain info changes
    this.subdomainService.getSubdomainInfo$().subscribe(info => {
      this.subdomainInfo = info;
    });
  }

  testSubdomainDetection(): void {
    this.subdomainService.getSubdomainInfo().subscribe({
      next: (info) => {
        this.subdomainInfo = info;
        console.log('Subdomain info from backend:', info);
      },
      error: (error) => {
        console.error('Error getting subdomain info:', error);
        this.subdomainInfo = null;
      }
    });
  }

  getDebugInfo(): void {
    this.subdomainService.getDebugInfo().subscribe({
      next: (info) => {
        this.debugInfo = info;
        console.log('Debug info:', info);
      },
      error: (error) => {
        console.error('Error getting debug info:', error);
        this.debugInfo = { error: error.message };
      }
    });
  }

  refreshInfo(): void {
    this.subdomainService.refreshSubdomainInfo();
    this.debugInfo = null;
  }
}