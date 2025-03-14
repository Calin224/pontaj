import { Proiect } from "./proiect";
import { User } from "./user";

export type UtilizatoriProiect = {
    utilizatorId: string;
    utilizator: User;
    proiectId: number;
    proiect: Proiect;
}