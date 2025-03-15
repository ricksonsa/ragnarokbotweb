import { ScumServer, ScumServerResponse } from "./scum-server";

export class AccountDto {
    name!: string;
    email!: string;
    serverId!: number;
    server?: ScumServer;
    servers!: ScumServerResponse;
    accessLevel!: number;
}