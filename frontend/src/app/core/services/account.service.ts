import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { User } from '../../shared/models/user';
import { map } from 'rxjs';
import {environment} from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  baseUrl = environment.apiUrl;
  http = inject(HttpClient);
  currentUser = signal<User | null>(null);

  login(values: any){
    let params = new HttpParams();
    params = params.append('useCookies', true);

    return this.http.post(this.baseUrl + 'login', values, {params, withCredentials: true});
  }

  register(values: any){
    return this.http.post(this.baseUrl + 'account/register', values);
  }

  logout(){
    return this.http.post(this.baseUrl + 'account/logout', {}, {withCredentials: true});
  }

  getUserInfo(){
    return this.http.get<User>(this.baseUrl + 'account/user-info', {withCredentials: true}).pipe(
      map(user => {
        this.currentUser.set(user);
        return user;
      })
    )
  }

  getAuthStatus(){
    return this.http.get<{ isAuthenticated: boolean }>(this.baseUrl + 'account/auth-status', {withCredentials: true});
  }
}
