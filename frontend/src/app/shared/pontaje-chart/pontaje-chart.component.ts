// src/app/pontaje/pontaje-chart/pontaje-chart.component.ts
import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChartModule } from 'primeng/chart';
import { CardModule } from 'primeng/card';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { PontajDto } from '../models/pontajDto';
import { TimpDisponibilService, TimpDisponibil } from '../../core/services/timp-disponibil.service';

@Component({
  selector: 'app-pontaje-chart',
  standalone: true,
  imports: [CommonModule, ChartModule, CardModule, ProgressSpinnerModule],
  template: `
    <p-card header="Distribuția Orelor în Lună">
      <div *ngIf="loading" class="flex justify-content-center">
        <p-progressSpinner></p-progressSpinner>
      </div>
      <p-chart *ngIf="!loading" type="pie" [data]="chartData" [options]="chartOptions" width="100%" height="350px"></p-chart>
    </p-card>
  `,
  styles: [`
    :host {
      display: block;
      margin: 1rem 0;
    }
  `]
})
export class PontajeChartComponent implements OnChanges, OnInit {
  @Input() pontaje: PontajDto[] = [];
  @Input() lunaSelectata: Date = new Date();

  chartData: any;
  chartOptions: any;
  loading = false;
  timpDisponibil: TimpDisponibil | null = null;

  constructor(private timpDisponibilService: TimpDisponibilService) {
    this.initChart();
  }

  ngOnInit(): void {
    this.loadTimpDisponibil();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['lunaSelectata'] && !changes['lunaSelectata'].firstChange) {
      this.loadTimpDisponibil();
    } else if (changes['pontaje'] && this.timpDisponibil) {
      this.updateChartData();
    }
  }

  private loadTimpDisponibil(): void {
    this.loading = true;
    this.timpDisponibilService.getTimpDisponibil(this.lunaSelectata).subscribe({
      next: (data) => {
        this.timpDisponibil = data;
        this.updateChartData();
        this.loading = false;
      },
      error: (err) => {
        console.error('Eroare la încărcarea timpului disponibil:', err);
        this.loading = false;
      }
    });
  }

  private initChart(): void {
    this.chartOptions = {
      plugins: {
        legend: {
          position: 'right',
          labels: {
            font: {
              size: 14
            }
          }
        },
        tooltip: {
          callbacks: {
            label: (tooltipItem: any) => {
              return `${tooltipItem.label}: ${tooltipItem.raw.toFixed(1)} ore`;
            }
          }
        }
      },
      responsive: true,
      maintainAspectRatio: false
    };

    // Inițializare cu valori goale
    this.chartData = {
      labels: [],
      datasets: [
        {
          data: [],
          backgroundColor: [
            '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF',
            '#FF9F40', '#5AD3D1', '#A569BD', '#5DADE2', '#48C9B0',
            '#E6E6FA' // Culoare pentru timpul disponibil
          ]
        }
      ]
    };
  }

  private updateChartData(): void {
    if (!this.pontaje?.length || !this.timpDisponibil) {
      return;
    }

    const proiecteMap = new Map<string, number>();

    // Calculăm orele pentru fiecare proiect
    this.pontaje.forEach(pontaj => {
      if (!pontaj.normaBaza && pontaj.oraStart && pontaj.oraFinal) { // Excludem pontajele cu normă de bază
        const proiect = pontaj.numeProiect || 'Necunoscut';
        const oreTotale = this.calculeazaOre(pontaj.oraStart, pontaj.oraFinal);
        const oreExistente = proiecteMap.get(proiect) || 0;
        proiecteMap.set(proiect, oreExistente + oreTotale);
      }
    });

    const labels: string[] = [];
    const data: number[] = [];

    // Adăugăm fiecare proiect
    proiecteMap.forEach((ore, proiect) => {
      labels.push(proiect);
      data.push(parseFloat(ore.toFixed(1))); // Rotunjim la 1 zecimală
    });

    // Adăugăm timpul disponibil rămas din backend
    if (this.timpDisponibil && this.timpDisponibil.oreRamaseLuna > 0) {
      labels.push('Timp disponibil rămas');
      data.push(parseFloat(this.timpDisponibil.oreRamaseLuna.toFixed(1)));
    }

    this.chartData = {
      labels: labels,
      datasets: [
        {
          data: data,
          backgroundColor: [
            '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF',
            '#FF9F40', '#5AD3D1', '#A569BD', '#5DADE2', '#48C9B0',
            '#E6E6FA' // Culoare pentru timpul disponibil
          ]
        }
      ]
    };
  }

  private calculeazaOre(oraInceput: string, oraSfarsit: string): number {
    if (!oraInceput || !oraSfarsit) {
      return 0;
    }

    const startParts = oraInceput.split(':').map(Number);
    const endParts = oraSfarsit.split(':').map(Number);

    const startMinutes = startParts[0] * 60 + startParts[1];
    const endMinutes = endParts[0] * 60 + endParts[1];

    const durationMinutes = endMinutes - startMinutes;
    return durationMinutes / 60;
  }
}
