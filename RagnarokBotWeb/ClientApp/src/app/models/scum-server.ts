import { DiscordDto } from "./discord.dto";
import { Ftp } from "./ftp";

export class ScumServer {
    id: number = 0;
    name?: string;
    ftp?: Ftp;
    discord?: DiscordDto;
}

export class ScumServerResponse {
    $values!: ScumServer[];
}