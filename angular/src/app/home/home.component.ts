import { AuthService } from '@abp/ng.core';
import { Component, OnInit } from '@angular/core';
import { HomePageSectionService } from '../proxy/application/home-page-content/home-page-section.service';
import { HomePageSectionDto } from '../proxy/application/contracts/home-page-content/models';
import { HomePageSectionType } from '../proxy/home-page-content/home-page-section-type.enum';
import { UploadedFileService } from '../proxy/application/files/uploaded-file.service';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  standalone: false,
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements OnInit {
  sections: HomePageSectionDto[] = [];
  loading = true;
  HomePageSectionType = HomePageSectionType;
  imageUrls = new Map<string, string>(); // Cache for image URLs

  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated;
  }

  constructor(
    private authService: AuthService,
    private homePageSectionService: HomePageSectionService,
    private uploadedFileService: UploadedFileService
  ) {}

  ngOnInit(): void {
    this.loadSections();
  }

  loadSections(): void {
    this.loading = true;
    this.homePageSectionService.getActiveForDisplay().subscribe({
      next: (sections) => {
        this.sections = sections || [];
        this.loadImages();
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  private loadImages(): void {
    const sectionsWithImages = this.sections.filter(s => s.imageFileId);

    if (sectionsWithImages.length === 0) {
      this.loading = false;
      return;
    }

    const imageRequests = sectionsWithImages.map(section =>
      this.uploadedFileService.get(section.imageFileId!).pipe(
        catchError(err => {
          console.error('Error loading image:', err);
          return of(null);
        })
      )
    );

    forkJoin(imageRequests).subscribe({
      next: (files) => {
        files.forEach((file, index) => {
          if (file && file.contentBase64) {
            const section = sectionsWithImages[index];
            const dataUrl = `data:${file.contentType};base64,${file.contentBase64}`;
            this.imageUrls.set(section.imageFileId!, dataUrl);
          }
        });
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  getImageUrl(imageFileId: string | undefined): string | undefined {
    if (!imageFileId) return undefined;
    return this.imageUrls.get(imageFileId);
  }

  login() {
    this.authService.navigateToLogin();
  }

  getSectionStyle(section: HomePageSectionDto): any {
    const style: any = {};
    if (section.backgroundColor) {
      style['background-color'] = section.backgroundColor;
    }
    if (section.textColor) {
      style['color'] = section.textColor;
    }
    return style;
  }
}
