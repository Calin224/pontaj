import { Component, EventEmitter, Input, Output } from '@angular/core';
import { DatePipe, NgClass, NgIf } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { BadgeModule } from 'primeng/badge';
import { MessageModule } from 'primeng/message';
import { PontajDto } from '../../../shared/models/pontajDto';

@Component({
  selector: 'app-pontaje-preview-dialog',
  standalone: true,
  imports: [
    DialogModule,
    ButtonModule,
    TableModule,
    ProgressSpinnerModule,
    NgIf,
    NgClass,
    DatePipe,
    BadgeModule,
    MessageModule
  ],
  template: `
    <p-dialog
      [header]="'Previzualizare pontaje: ' + numeProiect"
      [(visible)]="visible"
      [style]="{ width: '80vw' }"
      [dismissableMask]="true"
      [modal]="true"
      [closable]="!submitting"
      [draggable]="false"
      [resizable]="false"
      (onHide)="cancel.emit()"
    >
      <div class="p-fluid">
        <div *ngIf="loading" class="flex flex-col items-center justify-center py-8">
          <p-progressSpinner styleClass="w-12 h-12"></p-progressSpinner>
          <p class="mt-3 text-gray-600">Se generează previzualizarea...</p>
        </div>

        <div *ngIf="!loading && pontajePreview.length === 0" class="p-4 text-center">
          <p>Nu s-au putut genera pontaje pentru perioada și orele specificate.</p>
        </div>

        <div *ngIf="!loading && pontajePreview.length > 0" class="p-4">
          <h5 class="mb-2">Pontaje ce vor fi generate:</h5>

          <!-- Atenționare ore rămase neacoperite -->
          <div *ngIf="oreRamase > 0" class="mb-4 p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
            <div class="flex items-start">
              <i class="pi pi-exclamation-triangle text-yellow-500 mr-2 mt-1"></i>
              <div>
                <p class="text-sm font-semibold text-yellow-700 mb-1">Atenție: Nu s-au putut aloca toate orele solicitate!</p>
                <p class="text-sm text-yellow-600">
                  <span class="font-medium">{{oreAcoperite}} ore</span> au fost acoperite din totalul de <span class="font-medium">{{oreAcoperite + oreRamase}} ore</span> solicitate.
                </p>
                <p class="text-sm text-yellow-600">
                  <span class="font-medium">{{oreRamase}} ore</span> au rămas neacoperite.
                  Ar fi necesare aproximativ <span class="font-medium">{{zileNecesareExtra}} zile suplimentare</span> pentru a acoperi întregul necesar.
                </p>
              </div>
            </div>
          </div>

          <div class="flex items-center gap-2 mb-3 p-2 bg-gray-50 rounded text-sm">
            <span class="p-badge p-badge-warning">Înlocuiește normă</span>
            <span class="text-black">- Aceste pontaje vor înlocui ore din norma de bază</span>
          </div>

          <p-table [value]="pontajePreview" styleClass="p-datatable-sm">
            <ng-template pTemplate="header">
              <tr>
                <th>Data</th>
                <th>Interval orar</th>
                <th>Durata</th>
                <th>Status</th>
              </tr>
            </ng-template>
            <ng-template pTemplate="body" let-pontaj>
              <tr [ngClass]="{'bg-yellow-100': pontaj.inlocuiesteNorma || pontaj.InlocuiesteNorma}">
                <td>{{ pontaj.data | date:'dd.MM.yyyy' }}</td>
                <td>{{ formatTime(pontaj.oraStart) }} - {{ formatTime(pontaj.oraFinal) }}</td>
                <td>{{ calculateDuration(pontaj.oraStart, pontaj.oraFinal) }}</td>
                <td>
                  <span *ngIf="pontaj.inlocuiesteNorma || pontaj.InlocuiesteNorma" class="p-badge p-badge-warning">
                    Înlocuiește normă de bază
                  </span>
                  <span *ngIf="!(pontaj.inlocuiesteNorma || pontaj.InlocuiesteNorma)" class="p-badge p-badge-success">
                    Interval liber
                  </span>
                </td>
              </tr>
            </ng-template>
          </p-table>
        </div>
      </div>

      <ng-template pTemplate="footer">
        <div class="flex justify-end gap-2">
          <p-button
            label="Anulare"
            icon="pi pi-times"
            styleClass="p-button-outlined"
            [disabled]="submitting"
            (onClick)="cancel.emit()"
          ></p-button>
          <p-button
            *ngIf="pontajePreview.length > 0"
            label="Generare pontaje"
            icon="pi pi-check"
            styleClass="p-button-success"
            [loading]="submitting"
            (onClick)="confirm.emit()"
          ></p-button>
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class PontajePreviewDialogComponent {
  @Input() visible: boolean = false;
  @Input() pontajePreview: PontajDto[] = [];
  @Input() numeProiect: string = '';
  @Input() loading: boolean = false;
  @Input() submitting: boolean = false;

  // Proprietăți noi pentru informațiile suplimentare
  @Input() oreRamase: number = 0;
  @Input() oreAcoperite: number = 0;
  @Input() zileNecesareExtra: number = 0;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

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
