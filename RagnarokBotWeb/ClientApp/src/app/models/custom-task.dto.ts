export class CustomTaskDto {
    id: number;
    name: string;
    description?: string;
    enabled: boolean;
    isBlockPurchaseRaidTime: boolean;
    startMessage?: string;
    minPlayerOnline?: number;
    lastRunned?: Date;
    taskType: number;
    scumServerId: number;
    commands: string;
    cron: string;
}