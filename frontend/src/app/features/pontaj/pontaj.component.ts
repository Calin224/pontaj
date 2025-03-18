import {Component, inject, OnInit} from '@angular/core';
import {PontajService} from '../../core/services/pontaj.service';
import {Pontaj} from '../../shared/models/pontaj';
import {FormBuilder, FormsModule, ReactiveFormsModule} from '@angular/forms';
import {TableModule} from 'primeng/table';
import {Dialog, DialogModule} from 'primeng/dialog';
import {InputText} from 'primeng/inputtext';
import {ProiectService} from '../../core/services/proiect.service';
import {Proiect} from '../../shared/models/proiect';
import {SelectModule} from 'primeng/select';
import {DatePicker} from 'primeng/datepicker';
import {MatDatepicker, MatDatepickerInput, MatDatepickerToggle} from '@angular/material/datepicker';
import {MatFormField, MatLabel, MatSuffix} from '@angular/material/form-field';
import {MatNativeDateModule} from '@angular/material/core';
import {MatInput} from '@angular/material/input';
import {MessageService} from 'primeng/api';
import {Toast} from 'primeng/toast';
import {NgxMaterialTimepickerModule} from 'ngx-material-timepicker';

@Component({
  selector: 'app-pontaj',
  imports: [
    FormsModule,
    TableModule,
    ReactiveFormsModule,
    Dialog,
    SelectModule,
    MatDatepicker,
    MatDatepickerToggle,
    MatSuffix,
    MatFormField,
    MatLabel,
    MatDatepickerInput,
    MatNativeDateModule,
    MatInput,
    Toast,
    NgxMaterialTimepickerModule
  ],
  templateUrl: './pontaj.component.html',
  styleUrl: './pontaj.component.css',
  providers: [MessageService]
})
export class PontajComponent implements OnInit {
  constructor(private messageService: MessageService) {
  }

  private pontajService = inject(PontajService);
  private projectService = inject(ProiectService);

  pontaje: Pontaj[] = [];
  projects: Proiect[] = [];
  selectedDate: string = new Date().toISOString().split('T')[0];
  errorMessage: string | null = null;

  private fb = inject(FormBuilder);

  pontajForm = this.fb.group({
    data: [''],
    oraInceput: [''],
    oraSfarsit: [''],
    tipMunca: ['Norma de baza'],
    proiectId: null,
  });

  zileCuPontaje: any[] = [];

  dateClass = (d: Date): string => {
    const dateStr = d.toISOString().split('T')[0];
    return this.zileCuPontaje.includes(dateStr) ? 'highlight-day' : '';
  };

  visible: boolean = false;

  showDialog() {
    this.visible = true;
  }

  ngOnInit(): void {
    this.loadPontaje();
    this.loadProjects();

    this.pontajService.getZileCuPontaje(2025, 3).subscribe({
      next: res => {
        this.zileCuPontaje = res.map((d: number) => {
          const date = new Date(d);
          date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
          return date.toISOString().split('T')[0];
        });
      }
    })
  }

  loadPontaje() {
    this.pontajService
      .getPontajByDate(this.selectedDate)
      .subscribe({
        next: (pontaje) => {
          this.pontaje = pontaje || [];
          this.errorMessage = null;
        },
        error: err => {
          this.errorMessage = "In ziua selectata nu exista pontaje."
          this.pontaje = [];
        }
      })

  }

  loadProjects() {
    this.projectService.getProjects().subscribe((projects) => {
      this.projects = projects;
    })

  }

  onDateChange(event: any) {
    const sD = new Date(event.value);
    sD.setMinutes(sD.getMinutes() - sD.getTimezoneOffset());
    this.selectedDate = sD.toISOString().split('T')[0];
    this.loadPontaje();
  }

  onSubmit() {
    this.pontajService.addPontaj(this.pontajForm.value).subscribe({
      next: _ => {
        this.loadPontaje();
        this.pontajForm.reset();
        this.visible = false;
      },
      error: err => {
        this.messageService.add({severity: 'error', summary: 'Eroare', detail: 'Eroare la adaugarea pontajului.'});
      }
    });
  }

  deletePontaj(id: number){
    this.pontajService.deletePontaj(id).subscribe({
      next: _ => {
        console.log('Pontajul a fost sters');
        this.loadPontaje();
      },
      error: err => {
        console.log('Eroare la stergerea pontajului');
      }
    })
  }

  protected readonly Number = Number;
}
