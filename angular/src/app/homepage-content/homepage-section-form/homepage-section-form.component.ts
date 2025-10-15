import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToasterService } from '@abp/ng.theme.shared';
import { HomePageSectionService } from '../../proxy/application/home-page-content/home-page-section.service';
import { CreateHomePageSectionDto, UpdateHomePageSectionDto, HomePageSectionDto } from '../../proxy/application/contracts/home-page-content/models';
import { HomePageSectionType, homePageSectionTypeOptions } from '../../proxy/home-page-content/home-page-section-type.enum';
import { UploadedFileService } from '../../proxy/application/files/uploaded-file.service';
import { UploadFileDto } from '../../proxy/application/contracts/files/models';

@Component({
  selector: 'app-homepage-section-form',
  standalone: false,
  templateUrl: './homepage-section-form.component.html',
  styleUrls: ['./homepage-section-form.component.scss']
})
export class HomepageSectionFormComponent implements OnInit {
  form!: FormGroup;
  sectionId?: string;
  isEditMode = false;
  loading = false;
  saving = false;
  uploading = false;

  sectionTypeOptions = homePageSectionTypeOptions;
  HomePageSectionType = HomePageSectionType;

  // File upload properties
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  currentImageUrl: string | null = null;
  uploadedFileId: string | null = null;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private homePageSectionService: HomePageSectionService,
    private uploadedFileService: UploadedFileService,
    private toaster: ToasterService
  ) {}

  ngOnInit(): void {
    this.initializeForm();

    this.sectionId = this.route.snapshot.paramMap.get('id') || undefined;
    this.isEditMode = !!this.sectionId;

    if (this.isEditMode && this.sectionId) {
      this.loadSection(this.sectionId);
    }
  }

  private initializeForm(): void {
    this.form = this.fb.group({
      sectionType: [HomePageSectionType.HeroBanner, [Validators.required]],
      title: ['', [Validators.required, Validators.maxLength(200)]],
      subtitle: ['', [Validators.maxLength(500)]],
      content: ['', [Validators.maxLength(10000)]],
      imageFileId: [null], // Changed from imageUrl to imageFileId
      linkUrl: ['', [Validators.maxLength(2000)]],
      linkText: ['', [Validators.maxLength(100)]],
      validFrom: [null],
      validTo: [null],
      backgroundColor: [''],
      textColor: [''],
      isActive: [false]
    });
  }

  private loadSection(id: string): void {
    this.loading = true;

    this.homePageSectionService.get(id).subscribe({
      next: (section) => {
        this.form.patchValue({
          sectionType: section.sectionType,
          title: section.title,
          subtitle: section.subtitle,
          content: section.content,
          imageFileId: section.imageFileId,
          linkUrl: section.linkUrl,
          linkText: section.linkText,
          validFrom: section.validFrom ? new Date(section.validFrom) : null,
          validTo: section.validTo ? new Date(section.validTo) : null,
          backgroundColor: section.backgroundColor,
          textColor: section.textColor,
          isActive: section.isActive
        });

        // Load current image if exists
        if (section.imageFileId) {
          this.uploadedFileId = section.imageFileId;
          this.loadCurrentImage(section.imageFileId);
        }

        this.loading = false;
      },
      error: () => {
        this.toaster.error('MP::HomePageSection:LoadError', 'MP::Messages:Error');
        this.loading = false;
        this.cancel();
      }
    });
  }

  private loadCurrentImage(fileId: string): void {
    this.uploadedFileService.get(fileId).subscribe({
      next: (file) => {
        if (file.contentBase64) {
          this.currentImageUrl = `data:${file.contentType};base64,${file.contentBase64}`;
        }
      },
      error: (err) => {
        console.error('Error loading image:', err);
      }
    });
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      Object.keys(this.form.controls).forEach(key => {
        this.form.get(key)?.markAsTouched();
      });
      return;
    }

    this.saving = true;

    try {
      // Upload file first if a new file was selected
      if (this.selectedFile) {
        await this.uploadFile();
      }

      const formValue = this.form.value;

      const dto = {
        sectionType: formValue.sectionType,
        title: formValue.title,
        subtitle: formValue.subtitle || undefined,
        content: formValue.content || undefined,
        imageFileId: this.uploadedFileId || formValue.imageFileId || undefined,
        linkUrl: formValue.linkUrl || undefined,
        linkText: formValue.linkText || undefined,
        validFrom: formValue.validFrom ? formValue.validFrom.toISOString() : undefined,
        validTo: formValue.validTo ? formValue.validTo.toISOString() : undefined,
        backgroundColor: formValue.backgroundColor || undefined,
        textColor: formValue.textColor || undefined
      };

      if (this.isEditMode && this.sectionId) {
        this.homePageSectionService.update(this.sectionId, dto as UpdateHomePageSectionDto).subscribe({
          next: () => {
            this.toaster.success('MP::HomePageSection:UpdateSuccess');
            this.router.navigate(['/homepage-content']);
          },
          error: () => {
            this.toaster.error('MP::HomePageSection:UpdateError', 'MP::Messages:Error');
            this.saving = false;
          }
        });
      } else {
        this.homePageSectionService.create(dto as CreateHomePageSectionDto).subscribe({
          next: () => {
            this.toaster.success('MP::HomePageSection:CreateSuccess');
            this.router.navigate(['/homepage-content']);
          },
          error: () => {
            this.toaster.error('MP::HomePageSection:CreateError', 'MP::Messages:Error');
            this.saving = false;
          }
        });
      }
    } catch (error) {
      this.toaster.error('MP::HomePageSection:SaveError', 'MP::Messages:Error');
      this.saving = false;
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];

      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.toaster.error('MP::HomePageSection:InvalidFileType', 'MP::Messages:Error');
        return;
      }

      // Validate file size (max 10MB)
      const maxSize = 10 * 1024 * 1024; // 10MB
      if (file.size > maxSize) {
        this.toaster.error('MP::HomePageSection:FileTooLarge', 'MP::Messages:Error');
        return;
      }

      this.selectedFile = file;

      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        this.imagePreview = e.target?.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  private async uploadFile(): Promise<void> {
    if (!this.selectedFile) {
      return;
    }

    this.uploading = true;

    try {
      const base64 = await this.fileToBase64(this.selectedFile);

      const uploadDto: UploadFileDto = {
        fileName: this.selectedFile.name,
        contentType: this.selectedFile.type,
        contentBase64: base64,
        description: undefined
      };

      const uploadedFile = await this.uploadedFileService.upload(uploadDto).toPromise();

      if (uploadedFile?.id) {
        this.uploadedFileId = uploadedFile.id;
        this.form.patchValue({ imageFileId: uploadedFile.id });
      }
    } catch (error) {
      console.error('Upload error:', error);
      throw error;
    } finally {
      this.uploading = false;
    }
  }

  private fileToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => {
        const result = reader.result as string;
        // Remove data:image/...;base64, prefix
        const base64 = result.split(',')[1];
        resolve(base64);
      };
      reader.onerror = error => reject(error);
      reader.readAsDataURL(file);
    });
  }

  clearFileSelection(): void {
    this.selectedFile = null;
    this.imagePreview = null;
    const fileInput = document.getElementById('imageFile') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }

  removeImage(): void {
    this.currentImageUrl = null;
    this.uploadedFileId = null;
    this.form.patchValue({ imageFileId: null });
    this.clearFileSelection();
  }

  formatFileSize(bytes: number | undefined): string {
    if (!bytes) return '';
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  }

  cancel(): void {
    this.router.navigate(['/homepage-content']);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.form.get(fieldName);
    if (field?.hasError('required')) {
      return 'MP::Validation:Required';
    }
    if (field?.hasError('maxlength')) {
      const maxLength = field.errors?.['maxlength']?.requiredLength;
      return `MP::Validation:MaxLength::${maxLength}`;
    }
    return '';
  }
}
