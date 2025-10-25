import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PrimeNGModule } from '@shared/prime-ng.module';
import { RegistrationCodeInputComponent } from '@shared/components/registration-code-input/registration-code-input.component';

@Component({
  selector: 'app-no-unit-access',
  standalone: true,
  imports: [CommonModule, PrimeNGModule, RegistrationCodeInputComponent],
  templateUrl: './no-unit-access.component.html',
  styleUrls: ['./no-unit-access.component.scss'],
})
export class NoUnitAccessComponent {}
