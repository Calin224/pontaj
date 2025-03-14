import { Pontaj } from "./pontaj";
import { UtilizatoriProiect } from "./utilizator-proiect";

export type Proiect = {
    nume: string;
    utilizatorId: string;
    pontaje: Pontaj[];
}