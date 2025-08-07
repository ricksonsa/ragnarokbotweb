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
import { EventManager, EventWithContent } from '../../../services/event-manager.service';
import { Observable, of, debounceTime, distinctUntilChanged, tap, switchMap } from 'rxjs';
import { arrayBufferToBase64, toBase64 } from '../../../core/functions/file.functions';
import { AccountDto } from '../../../models/account.dto';
import { Alert } from '../../../models/alert';
import { ChannelDto } from '../../../models/channel.dto';
import { ItemDto } from '../../../models/item.dto';
import { PackItemDto, PackageDto } from '../../../models/package.dto';
import { AuthenticationService } from '../../../services/authentication.service';
import { ItemService } from '../../../services/item.service';
import { PackageService } from '../../../services/package.service';
import { ServerService } from '../../../services/server.service';
import { UavDto } from '../../../models/uav.dto';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-uav',
  templateUrl: './uav.component.html',
  styleUrls: ['./uav.component.scss'],
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
export class UavComponent implements OnInit {
  account?: AccountDto;
  packageForm!: FormGroup;
  private fb = inject(NonNullableFormBuilder);
  commands: string[] = [];
  isLoading: boolean;
  suggestions$: Observable<ItemDto[]> = of([]);
  autocompleteItems: string[] = [];
  channels: ChannelDto[] = [];
  avatarUrl: string;
  uploading = false;
  isUploaded = false;
  loading = false;
  apiUrl = environment.apiUrl;

  constructor(
    private readonly authService: AuthenticationService,
    private readonly serverService: ServerService,
    private readonly eventManager: EventManager) {
    this.packageForm = this.fb.group({
      id: [0],
      name: [null, [Validators.required]],
      description: [null],
      price: [null],
      vipPrice: [null],
      isVipOnly: [false],
      discordId: [null],
      imageUrl: [null],
      purchaseCooldownSeconds: [null],
      stockPerPlayer: [null],
      enabled: [true],
      isBlockPurchaseRaidTime: [false],
      deliveryText: [null],
      stockPerVipPlayer: [null],
      sendToUserDM: [false]
    });
  }

  ngOnInit() {
    this.loadAccount();
    this.loadDiscordChannels();
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

  loadAccount() {
    this.authService.account()
      .subscribe({
        next: (account) => {
          this.account = account;
          if (!account.server.discord) this.packageForm.controls['discordId'].disable();
          this.packageForm.patchValue(account.server.uav);
          this.isUploaded = true;
          this.avatarUrl = this.packageForm.value.imageUrl;
        }
      });
  }

  addComand(command: string) {
    if (command?.length === 0) return;
    this.commands.push(command);
  }

  removeCommand(index: number) {
    this.commands.splice(index, 1);
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

    this.loading = true;
    var uav = this.packageForm.value as UavDto;

    this.serverService.updateUav(uav)
      .subscribe({
        next: (value) => {
          const message = `UAV updated.`;
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', message, 'success')));
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
