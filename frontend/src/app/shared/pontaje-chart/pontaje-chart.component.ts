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
        console.log('Timp disponibil răspuns:', data);
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

    this.chartData = {
      labels: [],
      datasets: [
        {
          data: [],
          backgroundColor: [
            '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF',
            '#FF9F40', '#5AD3D1', '#A569BD', '#5DADE2', '#48C9B0',
            '#E6E6FA'
          ]
        }
      ]
    };
  }

  private updateChartData(): void {
    if (!this.timpDisponibil) {
      console.log('Timp disponibil nu există, nu se poate actualiza graficul');
      return;
    }

    console.log('Actualizare grafic cu:', this.timpDisponibil);

    // Calculăm orele pontate pe categorii
    const orePontatePerCategorie = new Map<string, number>();
    
    // Grupăm pontajele pe categorii (normă bază vs proiecte)
    if (this.pontaje && this.pontaje.length > 0) {
      // Inițializăm categoria "Normă de bază"
      orePontatePerCategorie.set('Normă de bază', 0);
      
      this.pontaje.forEach(pontaj => {
        if (pontaj.oraStart && pontaj.oraFinal) {
          const orePontaj = this.calculeazaOre(pontaj.oraStart, pontaj.oraFinal);
          
          if (pontaj.normaBaza) {
            // Adăugăm la categoria "Normă de bază"
            const oreNormaBaza = orePontatePerCategorie.get('Normă de bază') || 0;
            orePontatePerCategorie.set('Normă de bază', oreNormaBaza + orePontaj);
          } else {
            // Adăugăm la proiectul specific
            const numeProiect = pontaj.numeProiect || 'Necunoscut';
            const oreProiect = orePontatePerCategorie.get(numeProiect) || 0;
            orePontatePerCategorie.set(numeProiect, oreProiect + orePontaj);
          }
        }
      });
    }

    // Calculăm timpul disponibil rămas
    let oreRamaseLuna = 0;
    
    // Folosim datele din timpDisponibil dacă există
    if (this.timpDisponibil.oreRamaseLuna !== undefined) {
      oreRamaseLuna = this.timpDisponibil.oreRamaseLuna;
    } else if (this.timpDisponibil.oreDisponibileLuna !== undefined && this.timpDisponibil.orePontateLuna !== undefined) {
      // Calculăm manual dacă nu există direct
      oreRamaseLuna = this.timpDisponibil.oreDisponibileLuna - this.timpDisponibil.orePontateLuna;
    } else {
      // Valoare de rezervă - estimăm pe baza zilelor lucrătoare
      const zileLucratoare = 21; // Estimare standard
      oreRamaseLuna = zileLucratoare * 12 - Array.from(orePontatePerCategorie.values()).reduce((sum, ore) => sum + ore, 0);
    }
    
    console.log('Ore rămase calculate:', oreRamaseLuna);
    
    // Adăugăm timpul disponibil rămas, forțând o valoare pozitivă
    if (oreRamaseLuna > 0) {
      orePontatePerCategorie.set('Timp disponibil rămas', oreRamaseLuna);
    } else {
      console.log('Timp disponibil rămas este negativ sau zero:', oreRamaseLuna);
    }

    // Construim datele pentru grafic
    const labels: string[] = [];
    const data: number[] = [];

    orePontatePerCategorie.forEach((ore, categorie) => {
      labels.push(categorie);
      data.push(parseFloat(ore.toFixed(1)));
    });

    console.log('Date grafic finale:', { labels, data });

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