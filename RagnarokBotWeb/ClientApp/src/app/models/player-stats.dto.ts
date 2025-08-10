export interface PlayerStatsDto {
    steamId: string;
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