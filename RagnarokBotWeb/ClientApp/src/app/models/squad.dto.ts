export interface SquadMember {
    steamId: string;
    steamName: string;
    characterName: string;
    memberRank: number;
}

export interface Squad {
    squadId: number;
    squadName: string;
    members: SquadMember[];
}