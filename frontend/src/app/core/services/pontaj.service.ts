import {HttpClient, HttpParams} from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Pontaj } from '../../shared/models/pontaj';
import {GrupajPontaj} from '../../shared/models/grupajPontaj';
import {environment} from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PontajService {
  baseUrl = environment.apiUrl;
  http = inject(HttpClient);

  getPontajByDate(date: string){
    return this.http.get<Pontaj[]>(this.baseUrl + 'pontaj/zi/' + date, {withCredentials: true});
  }

  addPontaj(pontaj: any){
    return this.http.post(this.baseUrl + 'pontaj', pontaj, {withCredentials: true});
  }

  getPontajeByMonth(year: number, month:number){
    return this.http.get<GrupajPontaj[]>(this.baseUrl + 'pontaj/luna/' + year + '/' + month, {withCredentials: true});
  }

  getZileCuPontaje(year: number, month: number){
    let params = new HttpParams();
    params = params.append('year', year);
    params = params.append('month', month);

    return this.http.get<number[]>(this.baseUrl + 'pontaj/zile-pontaje', {params, withCredentials: true});
  }

  exortPontajExcel(year: number, month: number){
    let params = new HttpParams();
    params = params.append('year', year);
    params = params.append('month', month);
    return this.http.get(this.baseUrl + 'pontaj/export/', {params, responseType: 'blob', withCredentials: true});
  }

  deletePontaj(id: number){
    return this.http.delete(this.baseUrl + 'pontaj/' + id, {withCredentials: true});
  }
}
