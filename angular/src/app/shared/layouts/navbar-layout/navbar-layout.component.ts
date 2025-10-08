import { Component, OnInit, HostListener } from '@angular/core';
import { AuthService, ConfigStateService, PermissionService, RoutesService, LocalizationService, SessionStateService } from '@abp/ng.core';
import { Router } from '@angular/router';
import { Observable, map, combineLatest } from 'rxjs';

@Component({
  selector: 'app-navbar-layout',
  templateUrl: './navbar-layout.component.html',
  styleUrls: ['./navbar-layout.component.scss'],
  standalone: false
})
export class NavbarLayoutComponent implements OnInit {
  currentUser$: Observable<any>;
  isLoggedIn$: Observable<boolean>;
  currentTenant$: Observable<any>;
  appName: string;
  navigationItems$: Observable<any[]>;
  openDropdowns: Set<string> = new Set();
  availableLanguages: any[] = [];
  currentLanguage: string = 'en';
  isMobileMenuOpen: boolean = false;

  constructor(
    private authService: AuthService,
    private configState: ConfigStateService,
    private permissionService: PermissionService,
    private router: Router,
    private routesService: RoutesService,
    private localizationService: LocalizationService,
    private sessionState: SessionStateService
  ) {
    this.currentUser$ = this.configState.getOne$('currentUser');
    this.currentTenant$ = this.configState.getOne$('currentTenant');
    this.isLoggedIn$ = this.currentUser$.pipe(
      map(user => user?.isAuthenticated || false)
    );
    this.appName = 'MP';
  }

  ngOnInit(): void {
    // Initialize available languages - matching backend localization files
    this.availableLanguages = [
      { code: 'en', name: 'English', flag: '🇺🇸' },
      { code: 'pl-PL', name: 'Polski', flag: '🇵🇱' },
      { code: 'de-DE', name: 'Deutsch', flag: '🇩🇪' },
      { code: 'tr', name: 'Türkçe', flag: '🇹🇷' },
      { code: 'fr', name: 'Français', flag: '🇫🇷' },
      { code: 'es', name: 'Español', flag: '🇪🇸' },
      { code: 'it', name: 'Italiano', flag: '🇮🇹' },
      { code: 'cs', name: 'Čeština', flag: '🇨🇿' }
    ];

    // Get current language from session state
    this.sessionState.getLanguage$().subscribe(language => {
      this.currentLanguage = language || 'en';
    });

    this.navigationItems$ = combineLatest([
      this.routesService.flat$,
      this.currentTenant$,
      this.currentUser$
    ]).pipe(
      map(([routes, currentTenant, currentUser]) => {
        const isLoggedIn = currentUser?.isAuthenticated || false;

        // Filter routes for top-level menu items (no parentName)
        let topLevelRoutes = routes.filter(route =>
          route.path &&
          route.name &&
          !route.invisible &&
          !route.parentName
        );

        // Filter out protected routes if user is not logged in
        if (!isLoggedIn) {
          topLevelRoutes = topLevelRoutes.filter(route =>
            route.name !== '::Menu:BoothsManagement' &&
            route.name !== '::Menu:MyBooths' &&
            route.name !== '::Menu:Administration'
          );
        }

        // Add children to parent routes and check permissions
        const routesWithChildren = topLevelRoutes.map(route => {
          let children = routes.filter(childRoute =>
            childRoute.parentName === route.name &&
            !childRoute.invisible
          );

          // Filter out Tenants menu if user is logged in as tenant (has currentTenant set)
          if (currentTenant?.id) {
            children = children.filter(child =>
              child.name !== '::Menu:Tenants'
            );
          }

          // Filter children based on user permissions - only show items user has access to
          children = children.filter(child => {
            if (child.requiredPolicy) {
              return this.permissionService.getGrantedPolicy(child.requiredPolicy);
            }
            return true; // Show items without required policy
          });

          // Filter top-level routes based on custom permission logic
          if (route.name === '::Menu:BoothsManagement') {
            // Check if user has any of the management permissions
            const hasManagementPermission =
              this.permissionService.getGrantedPolicy('MP.Booths.Create') ||
              this.permissionService.getGrantedPolicy('MP.Booths.Edit') ||
              this.permissionService.getGrantedPolicy('MP.Booths.Delete');

            if (!hasManagementPermission) {
              return null;
            }
          }

          children = children.sort((a, b) => (a.order || 0) - (b.order || 0));

          // For Administration menu, only show if there are any visible children
          if (route.name === '::Menu:Administration') {
            // If no children after permission filtering, don't show Administration menu
            if (children.length === 0) {
              return null;
            }
          }

          return {
            ...route,
            children: children.length > 0 ? children : undefined
          };
        }).filter(route => route !== null); // Remove null routes

        return routesWithChildren.sort((a, b) => (a.order || 0) - (b.order || 0));
      })
    );
  }

  navigateToRegister(): void {
    // Create register URL with all OAuth parameters like the login flow
    const returnUrl = window.location.origin; // Remove trailing slash and pathname

    // Build OAuth authorization URL for register
    const authUrl = new URL('https://localhost:44377/connect/authorize');
    authUrl.searchParams.set('response_type', 'code');
    authUrl.searchParams.set('client_id', 'MP_App_KISS');
    authUrl.searchParams.set('redirect_uri', returnUrl);
    authUrl.searchParams.set('scope', 'openid offline_access MP');
    authUrl.searchParams.set('state', Math.random().toString(36).substring(2, 15));
    authUrl.searchParams.set('nonce', Math.random().toString(36).substring(2, 15));
    authUrl.searchParams.set('code_challenge', Math.random().toString(36).substring(2, 15));
    authUrl.searchParams.set('code_challenge_method', 'S256');
    authUrl.searchParams.set('culture', 'en');
    authUrl.searchParams.set('ui-culture', 'en');

    // Create register URL with OAuth parameters
    const registerUrl = `https://localhost:44377/Account/Register?returnUrl=${encodeURIComponent(authUrl.toString())}`;
    window.location.href = registerUrl;
  }

  logout(): void {
    this.authService.logout().subscribe();
  }

  login(): void {
    this.authService.navigateToLogin();
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }

  toggleDropdown(dropdownId: string, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }

    if (this.openDropdowns.has(dropdownId)) {
      this.openDropdowns.delete(dropdownId);
    } else {
      // Close all other dropdowns
      this.openDropdowns.clear();
      // Open this dropdown
      this.openDropdowns.add(dropdownId);
    }
  }

  isDropdownOpen(dropdownId: string): boolean {
    return this.openDropdowns.has(dropdownId);
  }

  closeAllDropdowns(): void {
    this.openDropdowns.clear();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    // Close all dropdowns and mobile menu when clicking outside
    const target = event.target as HTMLElement;
    if (!target.closest('.dropdown') && !target.closest('.navbar-toggler')) {
      this.closeAllDropdowns();
      this.closeMobileMenu();
    }
  }

  isParentActive(route: any): boolean {
    if (!route.children || route.children.length === 0) {
      return false;
    }

    const currentUrl = this.router.url;
    return route.children.some((child: any) =>
      currentUrl.startsWith(child.path) || currentUrl === child.path
    );
  }

  changeLanguage(languageCode: string): void {
    // Use ABP's SessionStateService to change language
    this.sessionState.setLanguage(languageCode);

    // Also try LocalizationService registerLocale if available
    if (this.localizationService.registerLocale) {
      this.localizationService.registerLocale(languageCode).then(() => {
        // Language registered successfully
      }).catch((error) => {
        console.warn('Could not register locale:', error);
      });
    }

    // Reload the page to ensure all components are updated with new language
    setTimeout(() => {
      window.location.reload();
    }, 100);
  }

  getCurrentLanguageFlag(): string {
    const currentLang = this.availableLanguages.find(lang => lang.code === this.currentLanguage);
    return currentLang ? currentLang.flag : '🌐';
  }

  getCurrentLanguageName(): string {
    const currentLang = this.availableLanguages.find(lang => lang.code === this.currentLanguage);
    return currentLang ? currentLang.name : 'Language';
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
    // Close all dropdowns when toggling mobile menu
    if (this.isMobileMenuOpen) {
      this.closeAllDropdowns();
    }
  }

  closeMobileMenu(): void {
    this.isMobileMenuOpen = false;
  }
}