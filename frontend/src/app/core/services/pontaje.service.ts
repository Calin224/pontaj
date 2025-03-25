import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { PontajDto } from '../../shared/models/pontajDto';
import { GenerarePontajDto } from '../../shared/models/generarePontajDto';
import { GenerareNormaDto } from '../../shared/models/generareNormaDto';
import { PontajPreviewDto } from '../../shared/models/pontajPreviewDto';

@Injectable({
  providedIn: 'root',
})
export class PontajeService {
  baseUrl = "https://localhost:5001/api/";
  http = inject(HttpClient);

  getPontaje(start: Date, end: Date) {
    const startAdjusted = new Date(start);
    startAdjusted.setHours(0, 0, 0, 0);

    const endAdjusted = new Date(end);
    endAdjusted.setHours(23, 59, 59, 999);

    let params = new HttpParams()
      .set('start', startAdjusted.toISOString())
      .set('end', endAdjusted.toISOString());

    return this.http.get<PontajDto[]>(this.baseUrl + 'pontaje', { params, withCredentials: true });
  }

  getProiecte() {
    return this.http.get<string[]>(`${this.baseUrl}pontaje/proiecte`, {withCredentials: true});
  }

  generarePontajeProiect(dto: GenerarePontajDto) {
    const startDate = new Date(dto.dataInceput);
    const endDate = new Date(dto.dataSfarsit);

    startDate.setHours(0, 0, 0, 0);
    endDate.setHours(23, 59, 59, 999);

    const adjustedDto = {
      ...dto,
      dataInceput: startDate.toISOString(),
      dataSfarsit: endDate.toISOString()
    }

    return this.http.post<PontajDto[]>(this.baseUrl + 'pontaje/generare-pontaj', adjustedDto, {withCredentials: true});
  }

  generareNormaBaza(luna: Date){
    const primaZiLuna = new Date(luna.getFullYear(), luna.getMonth(), 1);

    const dto : GenerareNormaDto = {
      luna: primaZiLuna.toISOString()
    }

    return this.http.post<any>(this.baseUrl + 'pontaje/generare-norma', dto, {withCredentials: true});
  }
  
  deletePontaj(id: number){
    return this.http.delete<any>(this.baseUrl + 'pontaje' + id, {withCredentials: true});
  }

  simualrePontaje(dto: GenerarePontajDto){
    const startDate = new Date(dto.dataInceput);
    const endDate = new Date(dto.dataSfarsit);

    startDate.setHours(0, 0, 0, 0);
    endDate.setHours(23, 59, 59, 999);

    const adjustedDto = {
      ...dto,
      dataInceput: startDate.toISOString(),
      dataSfarsit: endDate.toISOString()
    }

    return this.http.post<PontajPreviewDto[]>(this.baseUrl + 'pontaje/simulare-pontaj', adjustedDto, {withCredentials: true});
  }

}
