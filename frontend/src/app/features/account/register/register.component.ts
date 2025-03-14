import { Component, inject } from '@angular/core';
import { AccountService } from '../../../core/services/account.service';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { FloatLabel } from 'primeng/floatlabel';
import { InputText } from 'primeng/inputtext';
import { Button } from 'primeng/button';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    FloatLabel,
    InputText,
    Button,
    RouterLink
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css',
  providers: [MessageService]
})
export class RegisterComponent {
  private accountService = inject(AccountService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  constructor(private messageService: MessageService) {  }

  registerForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  })

  onSubmit(){
    this.accountService.register(this.registerForm.value).subscribe({
      next: () => {
        // this.accountService.getUserInfo().subscribe();
        this.messageService.add({severity:'success', summary:'Success', detail:'Registration successful'});
        this.router.navigateByUrl('/account/login');
      }
    })
  }
}