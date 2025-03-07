import { ScumServer, ScumServerResponse } from "./scum-server";

export class AccountDto {
    email!: string;
    serverId!: number;
    server?: ScumServer;
    servers!: ScumServerResponse;
}