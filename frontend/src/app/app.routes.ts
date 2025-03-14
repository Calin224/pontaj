import { Routes } from '@angular/router';
import { LoginComponent } from './features/account/login/login.component';
import { RegisterComponent } from './features/account/register/register.component';

export const routes: Routes = [
    {path: "account/login", component:LoginComponent },
    {path: "account/register", component: RegisterComponent}, 
];
