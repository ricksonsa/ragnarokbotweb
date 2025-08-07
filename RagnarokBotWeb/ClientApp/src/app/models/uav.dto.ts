import { ItemDto } from "./item.dto";

export class UavDto {
    id!: number;
    name?: string;
    description?: string;
    price?: number;
    vipPrice?: number;
    commands?: string;
    imageUrl?: string;
    discordChannelId?: string;
    discordChannelName?: string;
    purchaseCooldownSeconds?: number;
    stockPerPlayer?: number;
    enabled!: boolean;
    isBlockPurchaseRaidTime!: boolean;
    isVipOnly!: boolean;
    createDate!: Date;
}