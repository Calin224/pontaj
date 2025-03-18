import {Routes} from '@angular/router';
import {LoginComponent} from './features/account/login/login.component';
import {RegisterComponent} from './features/account/register/register.component';
import {ProfileComponent} from './features/account/profile/profile.component';
import {PontajComponent} from './features/pontaj/pontaj.component';
import {PontajLunaComponent} from './features/pontaj-luna/pontaj-luna.component';
import {HomeComponent} from './shared/home/home.component';
import {authGuard} from './core/guards/auth.guard';

export const routes: Routes = [
  {path: "", pathMatch: "full", component: HomeComponent},
  {path: "account/login", component: LoginComponent},
  {path: "account/register", component: RegisterComponent},
  {path: "account/profile", component: ProfileComponent},
  {path: "pontaje", component: PontajComponent, canActivate: [authGuard]},
  {path: "pontaj-luna", component: PontajLunaComponent, canActivate: [authGuard]},
];
