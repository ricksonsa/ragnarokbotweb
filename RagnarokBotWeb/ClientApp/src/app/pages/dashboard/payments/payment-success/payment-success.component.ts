import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzResultModule } from 'ng-zorro-antd/result';
import { PaymentService } from '../../../../services/payment.service';
import { of, Subscription, switchMap } from 'rxjs';
import { AuthenticationService } from '../../../../services/authentication.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-payment-success',
  templateUrl: './payment-success.component.html',
  styleUrls: ['./payment-success.component.scss'],
  imports: [
    CommonModule,
    NzResultModule,
    NzCardModule,
    NzButtonModule
  ]
})
export class PaymentSuccessComponent implements OnInit, OnDestroy {
  token?: string;
  confirmed = false;
  subs: Subscription;
  timer: any;

  constructor(
    private route: ActivatedRoute,
    private readonly paymentService: PaymentService,
    private readonly accountService: AuthenticationService,
    private router: Router) { }


  ngOnInit() {
    this.token = this.route.snapshot.queryParams['token'];
    console.log('queryParams:', this.route.snapshot.queryParams);

    if (!this.token) {
      this.goPayments();
    } else {
      this.timer = setInterval(() => this.confirmPayment(), 10000);
    }
  }

  ngOnDestroy(): void {
    clearInterval(this.timer);
    this.subs?.unsubscribe();
  }

  goPayments() {
    this.router.navigate(['dashboard', 'payments']);
  }

  confirmPayment() {
    this.subs?.unsubscribe();
    this.subs = this.paymentService.getPaymentByToken(this.token)
      .pipe(switchMap(value => {
        if (value.status == 1) {
          clearInterval(this.timer);
          return this.accountService.account(true);
        }
        else {
          return of(null);
        }
      }))
      .subscribe({
        next: (result) => {
          // this.router.navigate(['dashboard', 'payments']);
        }
      });
  }

}
