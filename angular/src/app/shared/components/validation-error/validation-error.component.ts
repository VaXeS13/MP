import { Component, TemplateRef, ViewChild } from '@angular/core';
import { ValidationErrorComponent as ErrorComponent } from '@ngx-validate/core';

@Component({
  template: `
    <ng-template #validationErrorTemplate>
      <div class="invalid-feedback" *ngFor="let error of errors">
        {{ error.message }}
      </div>
    </ng-template>
  `,
  standalone: false
})
export class ValidationErrorComponent extends ErrorComponent {
  @ViewChild('validationErrorTemplate', { static: true })
  validationErrorTemplate!: TemplateRef<any>;
}