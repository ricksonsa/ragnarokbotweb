export class ExchangeDto {
    id!: number;
    name?: string;
    description?: string;
    imageUrl?: string;
    discordChannelId?: string;
    discordChannelName?: string;
    purchaseCooldownSeconds?: number;
    stockPerPlayer?: number;
    enabled!: boolean;
    isBlockPurchaseRaidTime!: boolean;
    isVipOnly!: boolean;
    createDate!: Date;
    allowWithdraw: boolean;
    allowDeposit: boolean;
    allowTransfer: boolean;
    transferRate: number;
    withdrawRate: number;
    depositRate: number;
}