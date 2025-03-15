import { PackageDto } from "./package.dto";
import { PlayerDto } from "./player.dto";
import { ScumServer } from "./scum-server";

export class OrderDto {
    id!: number;
    pack?: PackageDto;
    status: number;
    player?: PlayerDto;
    scumServer!: ScumServer;
}