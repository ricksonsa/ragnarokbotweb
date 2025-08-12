import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzResultModule } from 'ng-zorro-antd/result';
import { PaymentService } from '../../../../services/payment.service';
import { switchMap } from 'rxjs';
import { AuthenticationService } from '../../../../services/authentication.service';

@Component({
  selector: 'app-payment-success',
  templateUrl: './payment-success.component.html',
  styleUrls: ['./payment-success.component.scss'],
  imports: [
    NzResultModule,
    NzCardModule,
    NzButtonModule
  ]
})
export class PaymentSuccessComponent implements OnInit {
  token?: string;

  constructor(
    private route: ActivatedRoute,
    private readonly paymentService: PaymentService,
    private readonly accountService: AuthenticationService,
    private router: Router) { }

  ngOnInit() {
    this.token = this.route.snapshot.queryParams['token'];
    console.log('Token from snapshot:', this.token);

    if (!this.token) {
      this.goPayments();
    } else {
      this.confirmPayment();

    }
  }

  goPayments() {
    this.router.navigate(['dashboard', 'payments']);
  }

  confirmPayment() {
    this.paymentService.confirmPayment(this.token)
      .pipe(switchMap(value => {
        return this.accountService.account(true)
      }))
      .subscribe({
        next: (result) => {
          this.router.navigate(['dashboard', 'payments']);
        }
      });
  }

}
