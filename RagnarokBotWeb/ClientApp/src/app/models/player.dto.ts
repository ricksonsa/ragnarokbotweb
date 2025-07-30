import { ScumServer } from "./scum-server";

export class PlayerDto {
    id!: number;
    name?: string;
    scumId?: string;
    steamId64?: string;
    steamName?: string;
    discordId?: string;
    discordName?: string;
    scumServer?: ScumServer;
    money?: number;
    gold?: number;
    fame?: number;
    coin?: number;
    createDate?: number;
    isVip!: boolean;
    isBanned!: boolean;
    isSilenced!: boolean;
    vipExpiresAt?: Date;
    banExpiresAt?: Date;
    silenceExpiresAt?: Date;
    lastLoggedIn?: Date;
}