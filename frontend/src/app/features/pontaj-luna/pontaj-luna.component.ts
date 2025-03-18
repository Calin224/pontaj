import {Component, inject, OnInit} from '@angular/core';
import {PontajService} from '../../core/services/pontaj.service';
import {Pontaj} from '../../shared/models/pontaj';
import {TableModule} from 'primeng/table';
import {DatePipe, DecimalPipe, NgForOf} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {GrupajPontaj} from '../../shared/models/grupajPontaj';

@Component({
  selector: 'app-pontaj-luna',
  imports: [
    TableModule,
    DatePipe,
    FormsModule,
    NgForOf,
    DecimalPipe
  ],
  templateUrl: './pontaj-luna.component.html',
  styleUrl: './pontaj-luna.component.css'
})
export class PontajLunaComponent implements OnInit {
  private pontajService = inject(PontajService);

  pontaje: Pontaj[] = [];
  pontajeGrupate: GrupajPontaj[] = [];

  errorMessage: string = "";

  currentYear = new Date().getFullYear();
  currentMonth = new Date().getMonth() + 1;

  months = [
    'Ianuarie', 'Februarie', 'Martie', 'Aprilie', 'Mai', 'Iunie',
    'Iulie', 'August', 'Septembrie', 'Octombrie', 'Noiembrie', 'Decembrie'
  ];

  ngOnInit() {
    this.loadPontaje();
  }

  loadPontaje() {
    this.pontajService.getPontajeByMonth(this.currentYear, this.currentMonth).subscribe({
      next: pontaje => {
        this.pontajeGrupate = pontaje;
        if(this.pontajeGrupate.length === 0){
          this.errorMessage = "Nu exista pontaj pentru aceasta luna";
          console.log(this.errorMessage);
        }
      }
    })
  }

  generateExcel() {
    this.pontajService.exortPontajExcel(this.currentYear, this.currentMonth).subscribe({
      next: file => {
        const blob = new Blob([file], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Pontaj_${this.currentMonth}_${this.currentYear}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: err => {
        console.error('Eroare la exportul în Excel:', err);
      }
    });
  }
}

