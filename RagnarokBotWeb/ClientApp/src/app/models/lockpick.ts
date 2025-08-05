export interface LockpickLog {
    line: string;
    user: string;
    scumId: number;
    steamId: string;
    success: boolean;
    elapsedTime: number;
    failedAttempts: number;
    targetObject: string;
    targetId: string;
    lockType: string;
    ownerScumId: number;
    ownerSteamId: string;
    ownerName: string;
    date: Date;
    x: number;
    y: number;
    z: number;
}