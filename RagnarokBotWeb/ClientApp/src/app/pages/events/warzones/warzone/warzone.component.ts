import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzAutocompleteModule } from 'ng-zorro-antd/auto-complete';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzUploadFile, NzUploadModule } from 'ng-zorro-antd/upload';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzImageModule } from 'ng-zorro-antd/image';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { Observable, of, debounceTime, distinctUntilChanged, tap, switchMap, Observer } from 'rxjs';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { arrayBufferToBase64, toBase64 } from '../../../../core/functions/file.functions';
import { AccountDto } from '../../../../models/account.dto';
import { Alert } from '../../../../models/alert';
import { ChannelDto } from '../../../../models/channel.dto';
import { ItemDto } from '../../../../models/item.dto';
import { AuthenticationService } from '../../../../services/authentication.service';
import { EventManager, EventWithContent } from '../../../../services/event-manager.service';
import { ItemService } from '../../../../services/item.service';
import { ServerService } from '../../../../services/server.service';
import { WarzoneDto } from '../../../../models/warzone.dto';
import { WarzoneItemDto } from '../../../../models/warzone-item.dto';
import { WarzoneTeleportDto } from '../../../../models/warzone-teleport';
import { WarzoneSpawnDto } from '../../../../models/warzone-spawn.dto';
import { WarzoneService } from '../../../../services/warzone.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-warzone',
  templateUrl: './warzone.component.html',
  styleUrls: ['./warzone.component.scss'],
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
export class WarzoneComponent implements OnInit {
  account?: AccountDto;
  packageForm!: FormGroup;
  packageItemForm!: FormGroup;
  teleportForm!: FormGroup;
  teleports: WarzoneTeleportDto[] = [];
  itemSpawnPointForm!: FormGroup;
  itemSpawnPoints: WarzoneSpawnDto[] = [];
  private fb = inject(NonNullableFormBuilder);
  commands: string[] = [];
  items: WarzoneItemDto[] = [];
  isLoading: boolean;
  total: any;
  suggestions$: Observable<ItemDto[]> = of([]);
  autocompleteItems: string[] = [];
  selectedItem?: ItemDto;
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
    private readonly itemService: ItemService,
    private readonly warzoneService: WarzoneService,
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
      itemSpawnInterval: [5, [Validators.required, Validators.min(1)]],
      warzoneDurationInterval: [30, [Validators.required, Validators.min(5)]],
      minPlayerOnline: [null],
      isVipOnly: [false],
      discordChannelId: [null],
      startMessage: [null],
      imageUrl: [null],
      purchaseCooldownSeconds: [null],
      stockPerPlayer: [null],
      enabled: [true],
      isBlockPurchaseRaidTime: [false],
      deliveryText: ['A package was dropped at the warzone!'],
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

    this.itemSpawnPointForm = this.fb.group({
      name: [null, [Validators.required, Validators.minLength(1)]],
      coordinates: [null, [Validators.required, Validators.minLength(10)]]
    });
  }

  ngOnInit() {
    this.loadAccount();
    this.setUpFilter();
    this.loadDiscordChannels();

    this.route.data.subscribe(data => {
      var item = data['warzone'];
      if (item) {
        this.isUploaded = true;
        const warzone = item as WarzoneDto;
        this.packageForm.patchValue(item);
        this.avatarUrl = this.packageForm.value.imageUrl;
        this.items = warzone.warzoneItems;
        this.itemSpawnPoints = warzone.spawnPoints;
        this.teleports = warzone.teleports;
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
    this.warzoneService.deleteWarzone(id)
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

  autocompleteSelect(data: any) {
    this.selectedItem = data.nzValue;
  }

  changeFilter() {
    this.searchControl.setValue(this.filter);
  }

  addTeleport() {
    if (this.teleportForm.invalid) return;
    var teleport: WarzoneTeleportDto = {
      id: 0,
      warzoneId: this.packageForm.value.id,
      teleport: {
        id: 0,
        coordinates: this.teleportForm.value.coordinates,
        name: this.teleportForm.value.name
      }
    };

    this.teleports.push(teleport);

    this.teleportForm.patchValue({ name: null, coordinates: null });
  }

  addSpawnPoint() {
    if (this.itemSpawnPointForm.invalid) return;
    var teleport: WarzoneSpawnDto = {
      id: 0,
      warzoneId: this.packageForm.value.id,
      teleport: {
        id: 0,
        coordinates: this.itemSpawnPointForm.value.coordinates,
        name: this.itemSpawnPointForm.value.name
      }
    };
    this.itemSpawnPoints.push(teleport);

    this.itemSpawnPointForm.patchValue({ name: null, coordinates: null });
  }

  handleImageChange(data: any) { }

  setUpFilter() {
    this.suggestions$ = this.searchControl.valueChanges
      .pipe(
        debounceTime(300), // Wait 300ms after the last input
        distinctUntilChanged(), // Ignore same consecutive values
        tap(() => (this.isLoading = true)), // Show loading indicator
        switchMap(value => this.itemService.getItems(10, 1, value)
        ),
        tap((page) => {
          this.isLoading = false
        }),
        switchMap((page) => {
          return of(page.content);
        })
      ); // Hide loading indicator
  }

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
    this.router.navigate(['events', 'warzones']);
  }

  addItem() {
    if (!this.selectedItem) return;

    const alreadyItem = this.items.find(warzoneItem => warzoneItem.itemId == this.selectedItem.id);
    if (alreadyItem != null) return;

    var packageItem: WarzoneItemDto = {
      itemId: this.selectedItem.id,
      itemName: this.selectedItem.name,
      deleted: null,
      priority: this.packageItemForm.value.priority,
      warzoneId: this.packageForm.value.id
    };

    this.items.push(packageItem);

    this.selectedItem = null;
    this.packageItemForm.patchValue({
      searchControl: '',
      priority: 1,
      name: null
    });
  }

  addComand(command: string) {
    if (command?.length === 0) return;
    this.commands.push(command);
  }

  removeCommand(index: number) {
    this.commands.splice(index, 1);
  }

  removeItem(id: number) {
    this.items = this.items.filter(item => item.itemId !== id);
  }

  removeTeleport(index: number) {
    this.teleports.splice(index, 1);
  }

  remoteItemSpawnPoint(index: number) {
    this.itemSpawnPoints.splice(index, 1);
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

    if (!this.items || this.items.length <= 0) {
      this.eventManager.broadcast(new EventWithContent('alert', new Alert('', 'At least one item is required to create a Warzone.', 'error')));
      return;
    }

    if (!this.itemSpawnPoints || this.itemSpawnPoints.length <= 0) {
      this.eventManager.broadcast(new EventWithContent('alert', new Alert('', 'At least one item spawn point is required to create a Warzone.', 'error')));
      return;
    }

    if (!this.teleports || this.teleports.length <= 0) {
      this.eventManager.broadcast(new EventWithContent('alert', new Alert('', 'At least one teleport point is required to create a Warzone.', 'error')));
      return;
    }

    this.loading = true;
    var warzone = this.packageForm.value as WarzoneDto;
    warzone.warzoneItems = this.items;
    warzone.teleports = this.teleports;
    warzone.spawnPoints = this.itemSpawnPoints;

    this.warzoneService.saveWarzone(warzone)
      .subscribe({
        next: (value) => {
          var message = `Warzone ${value.name} successfully created.`;
          if (warzone.id) {
            message = `Warzone ${value.name} successfully updated.`;
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
