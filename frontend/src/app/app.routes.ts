import { Routes } from '@angular/router';
import { LoginComponent } from './features/account/login/login.component';
import { RegisterComponent } from './features/account/register/register.component';
import { ProfileComponent } from './features/account/profile/profile.component';
import { PontajComponent } from './features/pontaj/pontaj.component';
import {PontajLunaComponent} from './features/pontaj-luna/pontaj-luna.component';

export const routes: Routes = [
    {path: "account/login", component:LoginComponent },
    {path: "account/register", component: RegisterComponent},
    {path: "account/profile", component: ProfileComponent},
    {path: "pontaje", component: PontajComponent},
    {path: "pontaj-luna", component: PontajLunaComponent},
];
