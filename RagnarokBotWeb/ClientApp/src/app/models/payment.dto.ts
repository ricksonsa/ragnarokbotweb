import { SubscriptionDto } from "./subscription.dto";

export class PaymentDto {
    id: number;
    confirmDate?: Date;
    status: any;
    subscription: SubscriptionDto;
    url?: string;
    isExpired: boolean;
    orderNumber?: string;
    expireAt: Date;
}
