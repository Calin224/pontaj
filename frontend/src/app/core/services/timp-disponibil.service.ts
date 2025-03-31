import {inject, Injectable} from '@angular/core';
import {environment} from '../../../environments/environment';
import {HttpClient, HttpParams} from '@angular/common/http';
import { catchError, map, Observable, of, throwError } from 'rxjs';

export interface TimpDisponibil {
  oreDisponibileLuna: number;   // Total ore disponibile
  oreNormaBazaLuna: number;     // Ore standard pentru norma de bază
  oreProiecteLuna: number;      // Ore disponibile pentru proiecte
  orePontateLuna: number;       // Total ore pontate
  orePontateNormaBaza: number;  // Ore pontate pe norma de bază
  orePontateProiecte: number;   // Ore pontate pe proiecte
  oreRamaseLuna: number;        // Total ore rămase disponibile
  zileLucratoareLuna: number;   // Număr total de zile lucrătoare în lună
  zileLucratoareRamase: number; // Număr de zile lucrătoare rămase
  procentUtilizare: number;     // Procent de utilizare a timpului disponibil
}

@Injectable({
  providedIn: 'root'
})
export class TimpDisponibilService {
  baseUrl = environment.apiUrl;
  http = inject(HttpClient);

  getTimpDisponibil(luna: Date): Observable<TimpDisponibil> {
    const primaZiLuna = new Date(Date.UTC(luna.getFullYear(), luna.getMonth(), 1));
    const url = `${this.baseUrl}/timp-disponibil`;
    
    let params = new HttpParams().set('luna', primaZiLuna.toISOString());
    
    return this.http.get<any>(url, { 
      params, 
      withCredentials: true,
      observe: 'response' 
    }).pipe(
      map(response => {
        console.log('Răspuns primit:', response);
        return response.body as TimpDisponibil;
      }),
      catchError(error => {
        console.error('Eroare la obținerea timpului disponibil:', error);
        if (error.status === 200 && error.error) {
          console.log('Încercare de parsare a răspunsului din eroare:', error.error);
          return of(error.error as TimpDisponibil);
        }
        return throwError(() => error);
      })
    );
  }
}
