import { CommonModule, DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { ProgressSpinnerModule } from "primeng/progressspinner";
import { PontajDto } from '../../../shared/models/pontajDto';

@Component({
  selector: 'app-pontaje-preview-dialog',
  imports: [
    DialogModule,
    DatePipe,
    ButtonModule,
    ProgressSpinnerModule,
    MessageModule,
    CommonModule
  ],
  templateUrl: './pontaje-preview-dialog.component.html',
  styleUrl: './pontaje-preview-dialog.component.css'
})
export class PontajePreviewDialogComponent {

  @Input() visible = false;
  @Input() pontajePreview: PontajDto[] = [];
  @Input() numeProiect = '';
  @Input() loading = false;
  @Input() submitting = false;

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
  @Output() visibleChange = new EventEmitter<boolean>();

  formatTime(timeStr: string){
    if (!timeStr) return '';
    const parts = timeStr.split(':');
    return `${parts[0]}:${parts[1]}`;
  }

  calculateDuration(start: string, end: string): string {
    if (!start || !end) return '';
    
    const startParts = start.split(':').map(Number);
    const endParts = end.split(':').map(Number);

    const startMinutes = startParts[0] * 60 + startParts[1];
    const endMinutes = endParts[0] * 60 + endParts[1];

    const durationMinutes = endMinutes - startMinutes;
    const hours = Math.floor(durationMinutes / 60);
    const minutes = durationMinutes % 60;

    return `${hours}h${minutes > 0 ? ` ${minutes}m` : ''}`;
  }

  onConfirm(): void {
    this.confirm.emit();
  }

  onCancel(): void {
    this.visible = false;
    this.visibleChange.emit(false);
    this.cancel.emit();
  }
}
