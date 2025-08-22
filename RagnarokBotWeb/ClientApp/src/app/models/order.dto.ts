import { PackageDto } from "./package.dto";
import { PlayerDto } from "./player.dto";
import { ScumServer } from "./scum-server";
import { TaxiDto } from "./taxi.dto";
import { WarzoneDto } from "./warzone.dto";

export class OrderDto {
    id!: number;
    pack?: PackageDto;
    warzone?: WarzoneDto;
    taxi?: TaxiDto;
    status: number;
    orderType: number;
    player?: PlayerDto;
    scumServer!: ScumServer;
    createDate: Date;
}

export interface GraphDto {
    name: string;
    amount: number;
    color: string;
}