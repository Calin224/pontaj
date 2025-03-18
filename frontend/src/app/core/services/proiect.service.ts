import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Proiect } from '../../shared/models/proiect';

@Injectable({
  providedIn: 'root'
})
export class ProiectService {
  baseUrl = 'https://localhost:5001/api/';
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
