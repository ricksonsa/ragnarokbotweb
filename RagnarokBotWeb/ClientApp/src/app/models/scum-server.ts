import { DiscordDto } from "./discord.dto";
import { ExchangeDto } from "./exchange.dto";
import { Ftp } from "./ftp";
import { UavDto } from "./uav.dto";

export class ScumServer {
    id: number = 0;
    name?: string;
    ftp?: Ftp;
    discord?: DiscordDto;
    isCompliant: boolean;
    coinAwardPeriodically: number = 0;
    restartTimes: string[];
    battleMetricsId?: string;
    useKillFeed: boolean;
    showKillDistance: boolean;
    showKillSector: boolean;
    showKillWeapon: boolean;
    timeZoneId: string;
    showKillerName: boolean;
    showMineKill: boolean;
    showSameSquadKill: boolean;
    showKillOnMap: boolean;
    showKillCoordinates: boolean;
    killAnnounceText: string;
    slots?: number;
    uav?: UavDto;
    exchange?: ExchangeDto;
}

export class ScumServerResponse {
    $values!: ScumServer[];
}