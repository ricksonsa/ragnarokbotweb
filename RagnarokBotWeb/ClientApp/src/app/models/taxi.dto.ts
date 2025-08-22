import { ScumServer } from "./scum-server";
import { TaxiTeleportDto } from "./taxi-teleport";

export interface TaxiDto {
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
    minPlayerOnline: number | null;
    stockPerPlayer: number | null;
    enabled: boolean;
    isBlockPurchaseRaidTime: boolean;
    isVipOnly: boolean;
    scumServer: ScumServer;
    deleted: string | null;
    taxiTeleports: TaxiTeleportDto[];
    startMessage: string | null;
}