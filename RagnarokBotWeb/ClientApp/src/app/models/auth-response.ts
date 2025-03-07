import { ScumServer, ScumServerResponse } from "./scum-server";

export class AuthResponse {
    idToken?: string;
    accessToken?: string;
    scumServers!: ScumServer[];
}