import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzResultModule } from 'ng-zorro-antd/result';
import { PaymentService } from '../../../../services/payment.service';
import { EventManager, EventWithContent } from '../../../../services/event-manager.service';
import { Alert } from '../../../../models/alert';

@Component({
  selector: 'app-payment-cancel',
  templateUrl: './payment-cancel.component.html',
  styleUrls: ['./payment-cancel.component.scss'],
  imports: [
    NzResultModule,
    NzCardModule,
    NzButtonModule
  ]
})
export class PaymentCancelComponent implements OnInit {
  loading = false;

  constructor(private router: Router, private readonly paymentService: PaymentService, private readonly eventManager: EventManager) { }

  ngOnInit() {
  }

  goPayments() {
    this.router.navigate(['dashboard', 'payments']);
  }

  addPayment() {
    this.loading = true;
    this.paymentService.addPayment()
      .subscribe({
        next: (response) => {
          window.open("https://www.paypal.com/ncp/payment/ZJ2UNRCF4W2EL");
          this.loading = false;
        },
        error: (err) => {
          this.loading = false;
          this.eventManager.broadcast(new EventWithContent<Alert>('alert', new Alert('Error', err.error.details, 'error')));
        }
      });
  }

}
