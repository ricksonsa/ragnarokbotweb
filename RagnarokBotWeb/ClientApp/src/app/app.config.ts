import { ApplicationConfig, provideZoneChangeDetection, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { icons } from './icons-provider';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { en_US, provideNzI18n } from 'ng-zorro-antd/i18n';
import { DatePipe, registerLocaleData } from '@angular/common';
import en from '@angular/common/locales/en';
import { FormsModule } from '@angular/forms';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { httpInterceptorProviders } from './core/interceptor';
import {
  provideCharts,
  withDefaultRegisterables,
} from 'ng2-charts';
import { NzConfig, NZ_CONFIG } from 'ng-zorro-antd/core/config';
import { BarController, Colors, Legend, PieController } from 'chart.js';

const ngZorroConfig: NzConfig = {
  theme: {
    primaryColor: '#1890ff'
  }
};

provideCharts({ registerables: [BarController, Legend, Colors, PieController] })
registerLocaleData(en);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    { provide: NZ_CONFIG, useValue: ngZorroConfig },
    provideRouter(routes),
    provideNzIcons(icons),
    provideNzI18n(en_US),
    importProvidersFrom(FormsModule),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptorsFromDi()),
    provideCharts(withDefaultRegisterables()),
    DatePipe,
    httpInterceptorProviders
  ]
};
