import { ScumServer, ScumServerResponse } from "./scum-server";

export class AccountDto {
    name!: string;
    lastName!: string;
    email!: string;
    serverId!: number;
    server?: ScumServer;
    servers?: ScumServer[];
    accessLevel!: number;
}