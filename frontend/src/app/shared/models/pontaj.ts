import { Proiect } from "./proiect";
import {ZiDeLucru} from './ziDeLucru';

export type Pontaj = {
    ziDeLucru: ZiDeLucru;
    oraInceput: string;
    oraSfarsit: string;
    tipMunca: string;
    durataMuncita: string;
    proiect?: Proiect | null;
}
