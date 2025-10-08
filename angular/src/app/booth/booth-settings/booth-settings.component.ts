import { Component, OnInit } from '@angular/core';
import { BoothSettingsService, BoothSettingsDto } from '../../services/booth-settings.service';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-booth-settings',
  standalone: false,
  templateUrl: './booth-settings.component.html',
  styleUrls: ['./booth-settings.component.scss']
})
export class BoothSettingsComponent implements OnInit {
  settings: BoothSettingsDto = {
    minimumGapDays: 7
  };

  loading = false;
  saving = false;

  constructor(
    private boothSettingsService: BoothSettingsService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.loading = true;
    this.boothSettingsService.get().subscribe({
      next: (settings) => {
        this.settings = settings;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading booth settings:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load booth settings'
        });
        this.loading = false;
      }
    });
  }

  saveSettings(): void {
    this.saving = true;
    this.boothSettingsService.update(this.settings).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Booth settings saved successfully'
        });
        this.saving = false;
      },
      error: (error) => {
        console.error('Error saving booth settings:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to save booth settings'
        });
        this.saving = false;
      }
    });
  }
}
