import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ConfirmationService } from '@abp/ng.theme.shared';
import { ToasterService } from '@abp/ng.theme.shared';
import { HomePageSectionService } from '../../proxy/application/home-page-content/home-page-section.service';
import { HomePageSectionDto } from '../../proxy/application/contracts/home-page-content/models';
import { HomePageSectionType, homePageSectionTypeOptions } from '../../proxy/home-page-content/home-page-section-type.enum';

@Component({
  selector: 'app-homepage-section-list',
  standalone: false,
  templateUrl: './homepage-section-list.component.html',
  styleUrls: ['./homepage-section-list.component.scss']
})
export class HomepageSectionListComponent implements OnInit {
  sections: HomePageSectionDto[] = [];
  loading = false;

  sectionTypeOptions = homePageSectionTypeOptions;
  HomePageSectionType = HomePageSectionType;

  constructor(
    private homePageSectionService: HomePageSectionService,
    private router: Router,
    private confirmation: ConfirmationService,
    private toaster: ToasterService
  ) {}

  ngOnInit(): void {
    this.loadSections();
  }

  loadSections(): void {
    this.loading = true;
    this.homePageSectionService.getAllOrdered().subscribe({
      next: (result) => {
        this.sections = result || [];
        this.loading = false;
      },
      error: () => {
        this.toaster.error('MP::HomePageSection:LoadError', 'MP::Messages:Error');
        this.loading = false;
      }
    });
  }

  createNew(): void {
    this.router.navigate(['/homepage-content/new']);
  }

  edit(id: string): void {
    this.router.navigate(['/homepage-content', id, 'edit']);
  }

  delete(section: HomePageSectionDto): void {
    this.confirmation
      .warn('MP::HomePageSection:DeleteConfirm', 'MP::Common:Confirm')
      .subscribe((status) => {
        if (status === 'confirm') {
          this.homePageSectionService.delete(section.id!).subscribe({
            next: () => {
              this.toaster.success('MP::HomePageSection:DeleteSuccess');
              this.loadSections();
            },
            error: () => {
              this.toaster.error('MP::HomePageSection:DeleteError', 'MP::Messages:Error');
            }
          });
        }
      });
  }

  activate(section: HomePageSectionDto): void {
    this.homePageSectionService.activate(section.id!).subscribe({
      next: () => {
        this.toaster.success('MP::HomePageSection:ActivateSuccess');
        this.loadSections();
      },
      error: () => {
        this.toaster.error('MP::HomePageSection:ActivateError', 'MP::Messages:Error');
      }
    });
  }

  deactivate(section: HomePageSectionDto): void {
    this.homePageSectionService.deactivate(section.id!).subscribe({
      next: () => {
        this.toaster.success('MP::HomePageSection:DeactivateSuccess');
        this.loadSections();
      },
      error: () => {
        this.toaster.error('MP::HomePageSection:DeactivateError', 'MP::Messages:Error');
      }
    });
  }

  moveUp(section: HomePageSectionDto, index: number): void {
    if (index === 0) return;

    const sections = [...this.sections];
    [sections[index - 1], sections[index]] = [sections[index], sections[index - 1]];

    sections.forEach((s, i) => s.order = i);

    this.reorderSections(sections);
  }

  moveDown(section: HomePageSectionDto, index: number): void {
    if (index === this.sections.length - 1) return;

    const sections = [...this.sections];
    [sections[index], sections[index + 1]] = [sections[index + 1], sections[index]];

    sections.forEach((s, i) => s.order = i);

    this.reorderSections(sections);
  }

  private reorderSections(sections: HomePageSectionDto[]): void {
    const reorderList = sections.map(s => ({ id: s.id!, order: s.order }));

    this.homePageSectionService.reorder(reorderList).subscribe({
      next: () => {
        this.toaster.success('MP::HomePageSection:ReorderSuccess');
        this.loadSections();
      },
      error: () => {
        this.toaster.error('MP::HomePageSection:ReorderError', 'MP::Messages:Error');
      }
    });
  }

  getSectionTypeLabel(typeValue?: HomePageSectionType): string {
    if (typeValue === undefined) return 'Unknown';
    const option = this.sectionTypeOptions.find(o => o.value === typeValue);
    return option ? `MP::HomePageSectionType:${option.key}` : 'Unknown';
  }

  getValidityStatus(section: HomePageSectionDto): string {
    if (!section.validFrom && !section.validTo) {
      return 'Always Valid';
    }

    const now = new Date();
    const validFrom = section.validFrom ? new Date(section.validFrom) : null;
    const validTo = section.validTo ? new Date(section.validTo) : null;

    if (validFrom && now < validFrom) {
      return 'Not Started';
    }
    if (validTo && now > validTo) {
      return 'Expired';
    }
    return 'Valid';
  }

  isValidForDisplay(section: HomePageSectionDto): boolean {
    return section.isValidForDisplay;
  }
}
