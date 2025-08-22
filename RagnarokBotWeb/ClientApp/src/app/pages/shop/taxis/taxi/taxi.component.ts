import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzAutocompleteModule } from 'ng-zorro-antd/auto-complete';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzImageModule } from 'ng-zorro-antd/image';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { NzUploadFile, NzUploadModule } from 'ng-zorro-antd/upload';
import { Observable, of, debounceTime, distinctUntilChanged, tap, switchMap } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { arrayBufferToBase64, toBase64 } from '../../../../core/functions/file.functions';
import { AccountDto } from '../../../../models/account.dto';
import { Alert } from '../../../../models/alert';
import { ChannelDto } from '../../../../models/channel.dto';
import { WarzoneDto } from '../../../../models/warzone.dto';
import { AuthenticationService } from '../../../../services/authentication.service';
import { EventManager, EventWithContent } from '../../../../services/event-manager.service';
import { ServerService } from '../../../../services/server.service';
import { WarzoneService } from '../../../../services/warzone.service';
import { TaxiTeleportDto } from '../../../../models/taxi-teleport';
import { TaxiDto } from '../../../../models/taxi.dto';
import { TaxiService } from '../../../../services/taxi.service';

@Component({
  selector: 'app-taxi',
  templateUrl: './taxi.component.html',
  styleUrls: ['./taxi.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    ReactiveFormsModule,
    NzCardModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
    NzSpaceModule,
    NzIconModule,
    NzCheckboxModule,
    NzListModule,
    NzAutocompleteModule,
    NzAlertModule,
    NzTypographyModule,
    NzTableModule,
    NzPopconfirmModule,
    NzButtonModule,
    NzUploadModule,
    NzImageModule
  ]
})
export class TaxiComponent implements OnInit {

  account?: AccountDto;
  packageForm!: FormGroup;
  packageItemForm!: FormGroup;
  teleportForm!: FormGroup;
  teleports: TaxiTeleportDto[] = [];

  private fb = inject(NonNullableFormBuilder);
  commands: string[] = [];
  isLoading: boolean;
  total: any;
  filter = '';
  channels: ChannelDto[] = [];
  avatarUrl: string;
  uploading = false;
  isUploaded = false;
  loading = false;
  apiUrl = environment.apiUrl;

  get searchControl() {
    return this.packageItemForm.controls['searchControl'];
  }

  constructor(
    private readonly authService: AuthenticationService,
    private readonly taxiService: TaxiService,
    private readonly serverService: ServerService,
    private route: ActivatedRoute,
    private readonly eventManager: EventManager,
    private readonly router: Router) {

    this.packageForm = this.fb.group({
      id: [0],
      name: [null, [Validators.required]],
      description: [null],
      price: [null],
      vipPrice: [null],
      isVipOnly: [false],
      discordChannelId: [null],
      startMessage: [null],
      imageUrl: [null],
      purchaseCooldownSeconds: [null],
      stockPerPlayer: [null],
      enabled: [true],
      taxiType: [0, [Validators.required]],
      isBlockPurchaseRaidTime: [false],
      stockPerVipPlayer: [null]
    });

    this.packageItemForm = this.fb.group({
      searchControl: [null, [Validators.required]],
      priority: [3],
      name: [null, [Validators.required, Validators.minLength(1)]],
    });

    this.teleportForm = this.fb.group({
      name: [null, [Validators.required, Validators.minLength(1)]],
      coordinates: [null, [Validators.required, Validators.minLength(10)]]
    });
  }

  ngOnInit() {
    this.loadAccount();
    this.loadDiscordChannels();

    this.route.data.subscribe(data => {
      var item = data['taxi'];
      if (item) {
        this.isUploaded = true;
        const taxi = item as TaxiDto;
        this.packageForm.patchValue(item);
        this.avatarUrl = this.packageForm.value.imageUrl;
        this.teleports = taxi.taxiTeleports;
      }
    });
  }

  removeImage() {
    this.packageForm.controls['imageUrl'].patchValue(null);
    this.avatarUrl = null;
  }

  loadDiscordChannels() {
    this.serverService.getDiscordServer()
      .subscribe({
        next: (guild) => {
          this.channels = guild.channels;
        }
      });
  }

  confirmDelete(id: number) {
    this.taxiService.delete(id)
      .subscribe({
        next: () => {
          this.goBack();
        }
      });
  }

  resolvePriorityText(priority: number) {
    switch (priority) {
      case 1:
      default:
        return 'Low';
      case 2: return 'Medium';
      case 3: return 'High';
    }
  }

  cancelDelete() { }

  changeFilter() {
    this.searchControl.setValue(this.filter);
  }

  addTeleport() {
    if (this.teleportForm.invalid) return;
    var teleport: TaxiTeleportDto = {
      id: 0,
      taxiId: this.packageForm.value.id,
      teleport: {
        id: 0,
        coordinates: this.teleportForm.value.coordinates,
        name: this.teleportForm.value.name
      }
    };

    this.teleports.push(teleport);
    this.teleportForm.patchValue({ name: null, coordinates: null });
  }


  handleImageChange(data: any) { }

  loadAccount() {
    this.authService.account()
      .subscribe({
        next: (account) => {
          this.account = account;
          if (!account.server.discord) this.packageForm.controls['discordChannelId'].disable();
        }
      });
  }

  goBack() {
    this.router.navigate(['shop', 'taxis']);
  }

  addComand(command: string) {
    if (command?.length === 0) return;
    this.commands.push(command);
  }

  removeCommand(index: number) {
    this.commands.splice(index, 1);
  }

  removeTeleport(index: number) {
    this.teleports.splice(index, 1);
  }

  save() {
    if (this.packageForm.invalid) {
      Object.values(this.packageForm.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    if (!this.teleports || this.teleports.length <= 0) {
      this.eventManager.broadcast(new EventWithContent('alert', new Alert('', 'At least one teleport point is required to create a Taxi.', 'error')));
      return;
    }

    this.loading = true;
    var taxi = this.packageForm.value as TaxiDto;
    taxi.taxiTeleports = this.teleports;

    this.taxiService.save(taxi)
      .subscribe({
        next: (value) => {
          var message = `Taxi ${value.name} successfully created.`;
          if (taxi.id) {
            message = `Taxi ${value.name} successfully updated.`;
          }

          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', message, 'success')));
          this.goBack();
          this.loading = false;

        },
        error: (err) => {
          var msg = err.error?.details ?? 'One or more validation errors ocurred.';
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', msg, 'error')));
          this.loading = false;
        }
      });
  }

  imageFile: File | null = null;
  imagePreview: string | ArrayBuffer | null = null;
  handleBeforeUpload = (file: NzUploadFile, _fileList: NzUploadFile[]): boolean => {
    // Convert NzUploadFile to native File
    const rawFile = file as unknown as File;
    this.imageFile = rawFile;

    const reader = new FileReader();
    reader.onload = (e) => {
      this.imagePreview = e.target?.result;
      // toBase64(this.imageFile)
      this.avatarUrl = arrayBufferToBase64(e.target?.result as ArrayBuffer);
      toBase64(this.imageFile).then((value: string) => {
        this.avatarUrl = value;
        this.isUploaded = false;
        this.packageForm.controls['imageUrl'].setValue(value);
      });
    };
    reader.readAsDataURL(rawFile);

    return false; // prevent auto-upload
  };


}
