import { Proiect } from "./proiect";

export type Pontaj = {
    ziDeLucruId: number;
    data: Date;
    oraInceput: string;
    oraSfarsit: string;
    tipMunca: string;
    proiectId?: number;
    utilizatorId: string;
    proiect?: Proiect;
}