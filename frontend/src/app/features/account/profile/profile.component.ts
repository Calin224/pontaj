import { Component, inject, Input, OnInit } from '@angular/core';
import { Button } from 'primeng/button';
import { ProiectService } from '../../../core/services/proiect.service';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { Proiect } from '../../../shared/models/proiect';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';

@Component({
  selector: 'app-profile',
  imports: [
    ReactiveFormsModule,
    ToastModule,
    Dialog,
    InputText,
    CardModule
  ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css',
  providers: [MessageService]
})
export class ProfileComponent implements OnInit {
  private projectService = inject(ProiectService);
  private fb = inject(FormBuilder);

  constructor(private messageService: MessageService) {}

  projectForm = this.fb.group({
    denumireaActivitatii: [''],
    descriereaActivitatii: [''],
    pozitiaInProiect: [''],
    categoriaExpertului: [''],
    denumireBeneficiar: [''],
    titluProiect: [''],
    codProiect: ['']
  });


  projects: Proiect[] = [];

  visible: boolean = false;

  showDialog() {
    this.visible = true;
  }

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects(){
    this.projectService.getProjects().subscribe({
      next: projects => {
        this.projects = projects;
      }
    });
  }

  onSubmit(){
    this.projectService.createProject(this.projectForm.value).subscribe({
      next: _ => {
        this.messageService.add({severity:'success', summary:'Success', detail:'Proiectul a fost creat'});
        this.loadProjects();
        this.visible = false;
        this.projectForm.reset();
      }
    });
  }

  deleteProject(id: number) {
    this.projectService.deleteProject(id).subscribe({
      next: _ => {
        this.messageService.add({severity:'success', summary:'Success', detail:'Proiectul a fost sters'});
        this.loadProjects();
      }
    })
  }
}
