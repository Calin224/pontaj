import {Routes} from '@angular/router';
import {LoginComponent} from './features/account/login/login.component';
import {HomeComponent} from './shared/home/home.component';
import {authGuard} from './core/guards/auth.guard';
import { RegisterComponent } from './features/account/register/register.component';
import { PontajeComponent } from './features/pontaje/pontaje.component';
import {ProfileComponent} from './features/account/profile/profile.component';

export const routes: Routes = [
  {path: "", pathMatch: "full", component: HomeComponent},
  {path: "account/login", component: LoginComponent},
  {path: "account/register", component: RegisterComponent},
  {path: "account/profile", component: ProfileComponent, canActivate: [authGuard]},
  {path: "pontaje", component: PontajeComponent, canActivate: [authGuard]}
];
