import { ItemDto } from "./item.dto";

export interface WarzoneItemDto {
    itemId: number;
    itemName: string | null;
    warzoneId: number;
    priority: number;
    deleted: string | null;
}