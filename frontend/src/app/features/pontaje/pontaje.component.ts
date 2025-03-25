// src/app/pontaje/pontaje.component.ts
import { Component, inject, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { PontajeService } from '../../core/services/pontaje.service';
import { PontajDto } from '../../shared/models/pontajDto';
import { GenerarePontajDto } from '../../shared/models/generarePontajDto';
import { DatePipe, NgClass, NgIf } from '@angular/common';

import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { CalendarModule } from 'primeng/calendar';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { BadgeModule } from 'primeng/badge';
import { MessageModule } from 'primeng/message';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { DatePickerModule } from 'primeng/datepicker';
import { PontajePreviewDialogComponent } from './pontaje-preview-dialog/pontaje-preview-dialog.component';

@Component({
  selector: 'app-pontaje',
  imports: [
    FormsModule,
    NgClass,
    ReactiveFormsModule,
    NgIf,
    DatePipe,
    TableModule,
    ButtonModule,
    InputNumberModule,
    CalendarModule,
    AutoCompleteModule,
    ProgressSpinnerModule,
    BadgeModule,
    MessageModule,
    TooltipModule,
    DatePickerModule,
    PontajePreviewDialogComponent
  ],
  templateUrl: './pontaje.component.html',
  styleUrls: ['./pontaje.component.scss'],
  providers: [MessageService],
})
export class PontajeComponent implements OnInit {
  pontaje: PontajDto[] = [];
  pontajeFiltered: PontajDto[] = [];
  proiecte: string[] = [];
  loading = false;
  
  pontajePreview: PontajDto[] = [];
  showPreviewDialog = false;
  previewLoading = false;
  submitting = false;

  vizualizarePontajeCuNorma: boolean = true;
  
  protected pontajeService = inject(PontajeService);
  private fb = inject(FormBuilder);

  lunaSelectata: Date = new Date();

  constructor(private messageService: MessageService) {}

  ngOnInit(): void {
    this.loadProiecte();
    this.loadPontaje();
  }

  dataForm = this.fb.group({
    dataInceput: [new Date(), Validators.required],
    dataSfarsit: [new Date(), Validators.required],
  });

  proiecteForm = this.fb.group({
    numeProiect: ['', Validators.required],
    oreAlocate: [null, [Validators.required, Validators.min(1)]],
  });

  toggleNormaBaza() {
    this.vizualizarePontajeCuNorma = !this.vizualizarePontajeCuNorma;
  }

  loadProiecte(): void {
    this.pontajeService.getProiecte().subscribe(
      (proiecte: any) => {
        this.proiecte = proiecte;
      },
      (error: any) => {
        console.error('Eroare la încărcarea proiectelor:', error);
      }
    );
  }

  loadPontaje(): void {
    this.loading = true;
    const startDate = this.dataForm.get('dataInceput')?.value || new Date();
    const endDate = this.dataForm.get('dataSfarsit')?.value || new Date();

    this.pontajeService.getPontaje(startDate, endDate).subscribe(
      (pontaje: any) => {
        this.pontaje = pontaje;
        this.pontajeFiltered = this.pontaje.filter(p => p.normaBaza === false);
        this.loading = false;
      },
      (error: any) => {
        console.error('Eroare la încărcarea pontajelor:', error);
        this.loading = false;
      }
    );
  }

  onDateChange(): void {
    this.loadPontaje();
  }

  generareNormaBaza(): void {
    console.log('Generare norma de bază pentru luna:', this.lunaSelectata);

    if (!this.lunaSelectata) {
      this.messageService.add({
        severity: 'error',
        summary: 'Eroare',
        detail: 'Selectați luna pentru care se generează norma de bază!',
      });
      return;
    }

    const dataCorectata = new Date(this.lunaSelectata);
    dataCorectata.setMonth(dataCorectata.getMonth() + 1);

    this.loading = true;
    this.pontajeService.generareNormaBaza(dataCorectata).subscribe(
      () => {
        this.loadPontaje();
        this.messageService.add({
          severity: 'success',
          summary: 'Succes',
          detail: 'Norma de bază a fost generată cu succes!',
        });
      },
      (error: any) => {
        console.error('Eroare la generarea normei de bază:', error);
        this.loading = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Eroare',
          detail: 'Eroare la generarea normei de bază!',
        });
      }
    );
  }

  previzualizarePontajeProiect(): void {
    if (this.proiecteForm!.invalid) {
      return;
    }

    const startDate = this.dataForm.get('dataInceput')?.value || new Date();
    const endDate = this.dataForm.get('dataSfarsit')?.value || new Date();
    const numeProiect = this.proiecteForm!.get('numeProiect')?.value || '';

    const oreAlocateRaw = this.proiecteForm!.get('oreAlocate')?.value;

    let oreAlocate: number;
    if (oreAlocateRaw === null || oreAlocateRaw === undefined) {
      this.messageService.add({
        severity: 'error',
        summary: 'Eroare',
        detail: 'Introduceți numărul de ore!',
      });
      return;
    } else {
      oreAlocate = Number(oreAlocateRaw);
      if (isNaN(oreAlocate) || oreAlocate <= 0) {
        this.messageService.add({
          severity: 'error',
          summary: 'Eroare',
          detail: 'Numărul de ore trebuie să fie un număr pozitiv!',
        });
        return;
      }
    }

    const dto: GenerarePontajDto = {
      dataInceput: startDate.toISOString(),
      dataSfarsit: endDate.toISOString(),
      numeProiect: numeProiect,
      oreAlocate: oreAlocate,
    };

    this.showPreviewDialog = true;
    this.previewLoading = true;
    this.pontajeService.simualrePontaje(dto).subscribe({
      next: (pontaje: any) => {
        this.pontajePreview = pontaje;
        this.previewLoading = false;
      },
      error: (error: any) => {
        console.error('Eroare la previzualizarea pontajelor:', error);
        this.previewLoading = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Eroare',
          detail: 'Eroare la previzualizarea pontajelor!',
        });
      }
    });
  }

  onConfirmGenerarePontaje(): void {
    if (this.proiecteForm!.invalid) {
      return;
    }

    const startDate = this.dataForm.get('dataInceput')?.value || new Date();
    const endDate = this.dataForm.get('dataSfarsit')?.value || new Date();
    const numeProiect = this.proiecteForm!.get('numeProiect')?.value || '';
    const oreAlocate = Number(this.proiecteForm!.get('oreAlocate')?.value || 0);

    this.submitting = true;
    const dto: GenerarePontajDto = {
      dataInceput: startDate.toISOString(),
      dataSfarsit: endDate.toISOString(),
      numeProiect: numeProiect,
      oreAlocate: oreAlocate,
    };

    this.pontajeService.generarePontajeProiect(dto).subscribe({
      next: (_) => {
        this.loadPontaje();
        this.showPreviewDialog = false;
        this.submitting = false;
        this.messageService.add({
          severity: 'success',
          summary: 'Succes',
          detail: 'Pontajele au fost generate cu succes!',
        });
        // Resetează formularul de proiecte
        this.proiecteForm.reset();
      },
      error: (error) => {
        console.error('Eroare la generarea pontajelor:', error);
        this.submitting = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Eroare',
          detail: 'Eroare la generarea pontajelor!',
        });
      }
    });
  }

  // Metodă pentru anularea generării pontajelor
  onCancelGenerarePontaje(): void {
    this.showPreviewDialog = false;

    this.pontajePreview = [];
    this.previewLoading = false;
    this.submitting = false;
  }

  generarePontajeProiect(): void {
    // Acum doar afișează previzualizarea
    this.previzualizarePontajeProiect();
  }

  deletePontaj(id: number): void {
    if (confirm('Ești sigur că vrei să ștergi acest pontaj?')) {
      this.loading = true;
      this.pontajeService.deletePontaj(id).subscribe(
        () => {
          this.loadPontaje();
          this.messageService.add({
            severity: 'success',
            summary: 'Succes',
            detail: 'Pontajul a fost șters cu succes!',
          });
        },
        (error: any) => {
          console.error('Eroare la ștergerea pontajului:', error);
          this.loading = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Eroare',
            detail: 'Eroare la ștergerea pontajului!',
          });
        }
      );
    }
  }

  formatTime(timeStr: string): string {
    if (!timeStr) return '';
    const parts = timeStr.split(':');
    return `${parts[0]}:${parts[1]}`;
  }

  calculateDuration(start: string, end: string): string {
    const startParts = start.split(':').map(Number);
    const endParts = end.split(':').map(Number);

    const startMinutes = startParts[0] * 60 + startParts[1];
    const endMinutes = endParts[0] * 60 + endParts[1];

    const durationMinutes = endMinutes - startMinutes;
    const hours = Math.floor(durationMinutes / 60);
    const minutes = durationMinutes % 60;

    return `${hours}h${minutes > 0 ? ` ${minutes}m` : ''}`;
  }
}