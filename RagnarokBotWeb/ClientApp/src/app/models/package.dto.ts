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
    isWelcomePack!: boolean;
    isDailyPackage!: boolean;
    createDate!: Date;
    packItems?: PackItemDto[];
}

export interface PackItemDto {
    itemId: number;
    itemName: string;
    packId: number;
    amount: number;
    ammoCount: number;
    deleted: string | null;
}