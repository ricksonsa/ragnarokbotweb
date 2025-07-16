import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormGroup, FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { AuthenticationService } from '../../../../services/authentication.service';
import { AccountDto } from '../../../../models/account.dto';
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
import { PackageService } from '../../../../services/package.service';
import { EventManager, EventWithContent } from '../../../../services/event-manager.service';
import { Alert } from '../../../../models/alert';
import { ItemDto } from '../../../../models/item.dto';
import { ItemService } from '../../../../services/item.service';
import { Observable, of, debounceTime, distinctUntilChanged, tap, switchMap, Observer } from 'rxjs';
import { NzTableModule } from 'ng-zorro-antd/table';
import { PackageItemDto } from '../../../../models/package.dto';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { ServerService } from '../../../../services/server.service';
import { ChannelDto } from '../../../../models/channel.dto';
import { arrayBufferToBase64, toBase64 } from '../../../../core/functions/file.functions';

@Component({
  selector: 'app-package',
  templateUrl: './package.component.html',
  styleUrls: ['./package.component.scss'],
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
export class PackageComponent implements OnInit {
  account?: AccountDto;
  packageForm!: FormGroup;
  packageItemForm!: FormGroup;
  private fb = inject(NonNullableFormBuilder);
  commands: string[] = [];
  items: PackageItemDto[] = [];
  isLoading: boolean;
  total: any;
  suggestions$: Observable<ItemDto[]> = of([]);
  autocompleteItems: string[] = [];
  selectedItem?: ItemDto;
  filter = '';
  channels: ChannelDto[] = [];
  avatarUrl: string;
  uploading = false;

  get searchControl() {
    return this.packageItemForm.controls['searchControl'];
  }

  constructor(
    private readonly authService: AuthenticationService,
    private readonly packageService: PackageService,
    private readonly itemService: ItemService,
    private readonly serverService: ServerService,
    private route: ActivatedRoute,
    private readonly eventManager: EventManager,
    private readonly router: Router) {
    this.packageForm = this.fb.group({
      id: [null],
      name: [null, [Validators.required]],
      description: [null, [Validators.required]],
      price: [null],
      vipPrice: [null],
      isVipOnly: [false],
      discordChannelId: [null],
      imageUrl: [null],
      purchaseCooldownSeconds: [null],
      stockPerPlayer: [null],
      enabled: [true],
      isBlockPurchaseRaidTime: [false],
      deliveryText: ['{playerName} your order #{orderId} of {packageName} was delivered.']
    });

    this.packageItemForm = this.fb.group({
      searchControl: [null, [Validators.required]],
      amount: [1],
      ammoCount: [0]
    });
  }

  ngOnInit() {
    this.loadAccount();
    this.setUpFilter();
    this.loadDiscordChannels();

    this.route.data.subscribe(data => {
      var item = data['package'];
      if (item) {
        this.packageForm.patchValue(item);
        this.avatarUrl = this.packageForm.value.imageUrl;
        this.items = item.items;
      }
    });
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
    this.packageService.deletePackage(id)
      .subscribe({
        next: () => {
          this.goBack();
        }
      });
  }

  cancelDelete() { }

  autocompleteSelect(data: any) {
    this.selectedItem = data.nzValue;
  }

  changeFilter() {
    this.searchControl.setValue(this.filter);
  }

  handleImageChange(data: any) {

  }

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
    this.router.navigate(['servers', this.account!.serverId, 'shop', 'packages']);
  }

  addItem() {
    if (!this.selectedItem) return;

    var packageItem = new PackageItemDto(
      this.selectedItem.id,
      this.selectedItem.name,
      +this.packageItemForm.value.amount,
      +this.packageItemForm.value.ammoCount);

    this.items.push(packageItem);

    this.selectedItem = null;
    this.packageItemForm.patchValue({
      searchControl: '',
      amount: 1,
      ammoCount: 0
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
      this.eventManager.broadcast(new EventWithContent('alert', new Alert('', 'At least one item is required to create a package.', 'error')));
      return;
    }

    var pack = this.packageForm.value;
    pack.items = this.items;

    this.packageService.savePackage(pack)
      .subscribe({
        next: (value) => {
          var message = `Package ${value.name} successfully created.`;
          if (pack.id) {
            message = `Package ${value.name} successfully updated.`;
          }

          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', message, 'success')));
          this.goBack();
        },
        error: (err) => {
          var msg = err.error?.details ?? 'One or more validation errors ocurred.';
          this.eventManager.broadcast(new EventWithContent('alert', new Alert('', msg, 'error')));
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
        this.packageForm.controls['imageUrl'].setValue(value);
      });
    };
    reader.readAsDataURL(rawFile);

    return false; // prevent auto-upload
  };


}
