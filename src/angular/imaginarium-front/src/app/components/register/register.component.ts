import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  registerForm: FormGroup;
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      username: ['', [Validators.required, Validators.minLength(3)]],
      first_name: [''],
      last_name: [''],
      password: ['', [Validators.required, Validators.minLength(8)]],
      password_confirm: ['', [Validators.required]]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const passwordConfirm = control.get('password_confirm');

    if (!password || !passwordConfirm) {
      return null;
    }

    return password.value === passwordConfirm.value ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.markFormGroupTouched(this.registerForm);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const formValue = this.registerForm.value;

    this.authService.register(formValue).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: (error) => {
        this.isLoading.set(false);
        if (error.error) {
          const errorObj = error.error;
          if (errorObj.email) {
            this.errorMessage.set(`Email: ${Array.isArray(errorObj.email) ? errorObj.email[0] : errorObj.email}`);
          } else if (errorObj.username) {
            this.errorMessage.set(`Nazwa użytkownika: ${Array.isArray(errorObj.username) ? errorObj.username[0] : errorObj.username}`);
          } else if (errorObj.password) {
            this.errorMessage.set(`Hasło: ${Array.isArray(errorObj.password) ? errorObj.password[0] : errorObj.password}`);
          } else if (errorObj.detail) {
            this.errorMessage.set(errorObj.detail);
          } else {
            this.errorMessage.set('Wystąpił błąd podczas rejestracji. Spróbuj ponownie.');
          }
        } else {
          this.errorMessage.set('Wystąpił błąd podczas rejestracji. Spróbuj ponownie.');
        }
      }
    });
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }
}