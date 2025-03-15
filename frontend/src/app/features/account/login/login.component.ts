import {Component, inject} from '@angular/core';
import {AccountService} from '../../../core/services/account.service';
import {FormBuilder, ReactiveFormsModule} from '@angular/forms';
import {FloatLabel} from 'primeng/floatlabel';
import {InputText} from 'primeng/inputtext';
import {Button} from 'primeng/button';
import {Router} from '@angular/router';
import {CardModule} from 'primeng/card';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    FloatLabel,
    InputText,
    Button,
    CardModule,
    RouterLink
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  providers: [MessageService]
})
export class LoginComponent {
  private accountService = inject(AccountService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  constructor(private messageService: MessageService) {}

  loginForm = this.fb.group({
    email: [''],
    password: ['']
  })

  onSubmit(){
    this.accountService.login(this.loginForm.value).subscribe({
      next: _ => {
        // console.log("login successful");
        this.messageService.add({severity:'success', summary:'Login Successful', detail:'You have successfully logged in!'});
        this.router.navigateByUrl('');
        this.accountService.getUserInfo().subscribe();
      }
    })
  }
}