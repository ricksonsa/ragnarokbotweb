import { ItemDto } from "./item.dto";

export class PackageDto {
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
    isDailyPackage!: boolean;
    createDate!: Date;
    items?: ItemDto[];
}

