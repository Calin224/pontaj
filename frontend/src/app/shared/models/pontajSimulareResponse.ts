import {PontajDto} from './pontajDto';

export type PontajSimulareResponse = {
  pontaje: PontajDto[];
  oreRamase: number;
  oreAcoperite: number;
  zileNecesareExtra: number;
}
