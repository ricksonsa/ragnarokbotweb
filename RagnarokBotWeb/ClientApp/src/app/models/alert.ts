export class Alert {
    constructor(
        public readonly title: string,
        public readonly message: string,
        public readonly type?: 'error' | 'success' | 'warning' | 'info' | undefined
    ) {
    }
}