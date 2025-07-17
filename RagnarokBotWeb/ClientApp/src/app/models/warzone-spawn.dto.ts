import { Teleport } from "./teleport.dto";

export interface WarzoneSpawnDto {
    id: number;
    warzoneId: number;
    teleport: Teleport;
}