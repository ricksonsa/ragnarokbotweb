import { ScumServer } from "./scum-server";
import { WarzoneItemDto } from "./warzone-item.dto";
import { WarzoneSpawnDto } from "./warzone-spawn.dto";
import { WarzoneTeleportDto } from "./warzone-teleport";

export interface WarzoneDto {
    id: number;
    name: string;
    description: string;
    deliveryText: string | null;
    price: number;
    vipPrice: number;
    imageUrl: string | null;
    discordChannelId: string | null;
    discordMessageId: number | null;
    purchaseCooldownSeconds: number | null;
    warzoneDurationInterval: number;
    minPlayerOnline: number | null;
    itemSpawnInterval: number | null;
    stockPerPlayer: number | null;
    enabled: boolean;
    isBlockPurchaseRaidTime: boolean;
    isVipOnly: boolean;
    scumServer: ScumServer;
    deleted: string | null;
    warzoneItems: WarzoneItemDto[];
    teleports: WarzoneTeleportDto[];
    spawnPoints: WarzoneSpawnDto[];
    startMessage: string | null;
}