import { Component, TemplateRef, ViewChild } from '@angular/core';
import { ValidationErrorComponent as ErrorComponent } from '@ngx-validate/core';

@Component({
  template: `
    <ng-template #validationErrorTemplate>
      <div class="invalid-feedback" *ngFor="let error of errors; trackBy: trackByErrorMessage">
        {{ error.message }}
      </div>
    </ng-template>
  `,
  standalone: false
})
export class ValidationErrorComponent extends ErrorComponent {
  @ViewChild('validationErrorTemplate', { static: true })
  validationErrorTemplate!: TemplateRef<any>;

  trackByErrorMessage(index: number, error: any): string {
    return error.message || index.toString();
  }
}