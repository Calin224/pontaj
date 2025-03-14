import { Proiect } from "./proiect";
import { UtilizatoriProiect } from "./utilizator-proiect";

export type User = {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    proiecte: Proiect[];
}