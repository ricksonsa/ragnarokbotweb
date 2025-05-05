import { ChannelDto } from "./channel.dto";

export class GuildDto {
    discordName?: string;
    discordId?: string;
    token?: string;
    confirmed: boolean;
    enabled: boolean;
    serverId: number;
    discordLink?: string;
    channels: ChannelDto[];
    runTemplate: boolean;
}