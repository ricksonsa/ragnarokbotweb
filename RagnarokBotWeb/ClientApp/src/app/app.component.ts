import { Component, inject, OnInit, Renderer2 } from '@angular/core';
import { ActivatedRoute, Router, RouterLink, RouterOutlet } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { AuthenticationService } from './services/authentication.service';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, switchMap } from 'rxjs';
import { CommonModule } from '@angular/common';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzBreadCrumbModule } from 'ng-zorro-antd/breadcrumb';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzAffixModule } from 'ng-zorro-antd/affix';
import { NzListModule } from 'ng-zorro-antd/list';
import { EventManager, EventWithContent } from './services/event-manager.service';
import { Alert } from './models/alert';
import { AccountDto } from './models/account.dto';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { ThemeService } from './services/theme.service';


@Component({
  selector: 'app-root',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    RouterOutlet,
    NzIconModule,
    NzLayoutModule,
    NzButtonModule,
    NzInputModule,
    NzSpaceModule,
    NzFormModule,
    NzTypographyModule,
    NzAlertModule,
    NzCardModule,
    NzPopconfirmModule,
    NzSwitchModule,
    NzListModule,
    NzAvatarModule,
    NzBreadCrumbModule,
    NzAffixModule,
    NzMenuModule
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  isCollapsed = true;
  logged = false;
  register = false;
  loginForm!: FormGroup;
  registerForm!: FormGroup;
  errorMessage?: string;
  showAlert = true;
  alert?: Alert;
  account?: AccountDto;
  darkMode = false;
  loading = false;
  theme: string | null;

  private readonly notification = inject(NzNotificationService);
  paths: string[] = [];

  constructor(
    private readonly authenticationService: AuthenticationService,
    private readonly route: ActivatedRoute,
    private readonly themeService: ThemeService,
    private router: Router,
    private readonly eventManager: EventManager,
    private renderer: Renderer2,
    fb: FormBuilder
  ) {
    this.theme = localStorage.getItem("theme");
    themeService.setTheme(this.theme || 'light', renderer);
    this.darkMode = this.theme != 'light';

    this.loginForm = fb.group({
      email: [null, Validators.required],
      password: [null, Validators.required]
    });

    this.registerForm = fb.group({
      name: [null, Validators.required],
      email: [null, Validators.required],
      password: [null, Validators.required],
      confirmPassword: [null, Validators.required]
    });
    router.events.pipe(
      debounceTime(800),
      switchMap(value => {
        return this.authenticationService.account(true)
      }))
      .subscribe({
        next: (account) => {
          this.paths = router.url.replace(`servers/${account.serverId}/`, '').split('/');
          if (this.authenticationService.isAuthenticated()) {
            this.logged = true;
          }
        }
      });
  }

  changeTheme(value: boolean) {
    this.setTheme(value ? 'dark' : 'light');
  }

  setTheme(theme: string) {
    this.themeService.setTheme(theme, this.renderer);
    localStorage.setItem("theme", theme);
  }

  ngOnInit(): void {
    this.themeService.onThemeChange().subscribe({
      next: (theme) => {
        this.setTheme(theme);
      }
    });

    this.authenticationService.account(true)
      .subscribe({
        next: (account) => {
          if (this.authenticationService.isAuthenticated()) {
            this.logged = true;
            this.account = account;
          }
        }
      });

    this.authenticationService.logoutEvent.asObservable().subscribe({
      next: (value: boolean) => {
        this.logged = false;
      }
    });

    this.eventManager.subscribe('alert', ((event) => {
      var value = event as EventWithContent<Alert>;
      this.alert = value.content;
      this.showAlert = false;

      setTimeout(() => this.notification.create(
        this.alert.type,
        this.alert.title,
        this.alert.message
      ), 600);
      // this.showAlert = true;
      // setTimeout(() => this.showAlert = false, 5000);
    }));
  }

  getFirstLetter(input?: string) {
    if (!input) return 'A';
    return input[0].toUpperCase();
  }

  public login() {
    if (this.loginForm.invalid) {
      Object.values(this.loginForm.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    this.loading = true;
    this.authenticationService.authenticate(this.loginForm!.value)
      .pipe(
        (switchMap((value) => {
          return this.authenticationService.login(value.scumServers[0]!.id);
        })),
        switchMap(authResponse => {
          return this.authenticationService.account()
        })
      )
      .subscribe({
        next: (account) => {
          this.loading = false;
          this.logged = true;
          this.account = account;
        },
        error: (err) => {
          this.errorMessage = err.error.details
        }
      })
  }

  signup() {
    if (this.registerForm.invalid) {
      Object.values(this.registerForm.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    this.loading = true;
    this.authenticationService.register(this.registerForm.value)
      .subscribe({
        next: (value: any) => {
          this.register = false;
        }
      });
  }

  logout() {
    this.authenticationService.logout();
    this.logged = false;
    this.loading = false;
  }
}
