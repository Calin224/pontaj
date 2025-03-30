import {inject, Injectable} from '@angular/core';
import {environment} from '../../../environments/environment';
import {HttpClient, HttpParams} from '@angular/common/http';

export interface TimpDisponibil {
  oreDisponibileLuna: number;
  orePontateLuna: number;
  oreRamaseLuna: number;
  zileLucratoareLuna: number;
  zileLucratoareRamase: number;
  procentUtilizare: number;
}

@Injectable({
  providedIn: 'root'
})
export class TimpDisponibilService {
  baseUrl = environment.apiUrl;
  http = inject(HttpClient);

  getTimpDisponibil(luna?: Date) {
    let params = new HttpParams();

    if(luna){
      const year = luna.getFullYear();
      const month = (luna.getMonth() + 1).toString().padStart(2, '0');
      const day = luna.getDate().toString().padStart(2, '0');
      const formattedDate = `${year}-${month}-${day}`;
      params = params.append('luna', formattedDate);
    }

    return this.http.get<TimpDisponibil>(`${this.baseUrl}pontaje/timp-disponibil`, {params, withCredentials: true});
  }
}
