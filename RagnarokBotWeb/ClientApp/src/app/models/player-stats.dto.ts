export interface PlayerStatsDto {
    playerName: string;
    killCount: number;
}

export interface LockpickStatsDto {
    playerName: string
    lockType: string;
    successCount: number;
    failCount: number;
    successRate: number;
}