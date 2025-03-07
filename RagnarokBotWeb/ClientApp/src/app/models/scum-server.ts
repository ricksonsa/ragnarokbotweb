import { Ftp } from "./ftp";

export class ScumServer {
    id: number = 0;
    name?: string;
    ftp?: Ftp;
}

export class ScumServerResponse {
    $values!: ScumServer[];
}