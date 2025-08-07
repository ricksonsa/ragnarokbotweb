import { PackageDto } from "./package.dto";
import { PlayerDto } from "./player.dto";
import { ScumServer } from "./scum-server";
import { WarzoneDto } from "./warzone.dto";

export class OrderDto {
    id!: number;
    pack?: PackageDto;
    warzone?: WarzoneDto;
    status: number;
    orderType: number;
    player?: PlayerDto;
    scumServer!: ScumServer;
    createDate: Date;
}