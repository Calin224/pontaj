import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Proiect } from '../../shared/models/proiect';
import {environment} from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ProiectService {
  baseUrl = environment.apiUrl;
  http = inject(HttpClient);

  createProject(values: any){
    return this.http.post(this.baseUrl + 'proiect', values, {withCredentials: true});
  }

  getProjects(){
    return this.http.get<Proiect[]>(this.baseUrl + 'proiect', {withCredentials: true});
  }

  deleteProject(id: number){
    return this.http.delete(this.baseUrl + 'proiect', {withCredentials: true});
  }
}
