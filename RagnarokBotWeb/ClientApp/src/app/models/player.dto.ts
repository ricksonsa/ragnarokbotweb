import { ScumServer } from "./scum-server";

export class PlayerDto {
    name?: string;
    scumId?: string;
    steamId64?: string;
    steamName?: string;
    discordId?: string;
    scumServer?: ScumServer;
    money?: number;
    gold?: number;
    fame?: number;
    coin?: number;
    createDate?: number;
}