import { inject, Injectable } from '@angular/core';
import { AccountService } from './account.service';
import { catchError, forkJoin, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class InitService {
  accountService = inject(AccountService);

  init(){
    return forkJoin({
      user: this.accountService.getUserInfo()
    }).pipe(
      catchError(error => {
        console.error('Eroare la inițializare:', error);
        return of({ user: null });
      })
    ); 
  }
}
