import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  EnterpriseApiService,
  SystemHealthDashboard,
  BackupRecord,
  DatabaseMigrationSummary,
  BackgroundJobStatus,
  SystemConfigurationItem,
} from '../../../core/services/enterprise-api.service';

@Component({
  selector: 'magic-system-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './system-management.component.html',
  styleUrl: './system-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemManagementComponent implements OnInit {
  private readonly api = inject(EnterpriseApiService);

  protected readonly activeTab = signal<'health' | 'backups' | 'migrations' | 'jobs' | 'config'>('health');
  protected readonly health = signal<SystemHealthDashboard | null>(null);
  protected readonly backups = signal<readonly BackupRecord[]>([]);
  protected readonly migrations = signal<DatabaseMigrationSummary | null>(null);
  protected readonly jobs = signal<readonly BackgroundJobStatus[]>([]);
  protected readonly configurations = signal<readonly SystemConfigurationItem[]>([]);
  protected readonly loading = signal(false);
  protected readonly actionMessage = signal<string | null>(null);
  protected readonly runningBackup = signal(false);
  protected readonly simulatingRestore = signal(false);

  // Edit config modal
  protected readonly showConfigModal = signal(false);
  protected configForm = {
    key: '',
    value: '',
  };

  ngOnInit(): void {
    this.loadHealth();
  }

  protected setTab(tab: 'health' | 'backups' | 'migrations' | 'jobs' | 'config'): void {
    this.activeTab.set(tab);
    if (tab === 'health') this.loadHealth();
    if (tab === 'backups') this.loadBackups();
    if (tab === 'migrations') this.loadMigrations();
    if (tab === 'jobs') this.loadJobs();
    if (tab === 'config') this.loadConfigurations();
  }

  protected loadHealth(): void {
    this.loading.set(true);
    this.api.getSystemHealth().subscribe({
      next: (res) => {
        this.health.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadBackups(): void {
    this.loading.set(true);
    this.api.getBackups().subscribe({
      next: (res) => {
        this.backups.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadMigrations(): void {
    this.loading.set(true);
    this.api.getDatabaseMigrations().subscribe({
      next: (res) => {
        this.migrations.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadJobs(): void {
    this.loading.set(true);
    this.api.getBackgroundJobs().subscribe({
      next: (res) => {
        this.jobs.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadConfigurations(): void {
    this.loading.set(true);
    this.api.getConfigurations().subscribe({
      next: (res) => {
        this.configurations.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected createBackup(): void {
    this.runningBackup.set(true);
    this.api.createBackup().subscribe({
      next: (res) => {
        this.runningBackup.set(false);
        this.notify(`Backup created: ${res.fileName} (SHA-256: ${res.sha256Checksum.substring(0, 12)}...)`);
        this.loadBackups();
      },
      error: (err) => {
        this.runningBackup.set(false);
        this.notify(`Backup failed: ${err.message}`);
      },
    });
  }

  protected simulateRestore(backupId: string): void {
    this.simulatingRestore.set(true);
    this.api.simulateRestore(backupId).subscribe({
      next: (res) => {
        this.simulatingRestore.set(false);
        this.notify(`Restore simulation: ${res.message}`);
      },
      error: (err) => {
        this.simulatingRestore.set(false);
        this.notify(`Simulation failed: ${err.message}`);
      },
    });
  }

  protected openEditConfig(item: SystemConfigurationItem): void {
    if (item.isSecret) {
      this.notify('Secret parameters cannot be edited via standard UI');
      return;
    }
    this.configForm.key = item.key;
    this.configForm.value = item.value;
    this.showConfigModal.set(true);
  }

  protected saveConfig(): void {
    this.api.updateConfiguration(this.configForm.key, this.configForm.value).subscribe({
      next: () => {
        this.showConfigModal.set(false);
        this.notify('Configuration parameter saved');
        this.loadConfigurations();
      },
      error: (err) => this.notify(`Failed: ${err.message}`),
    });
  }

  private notify(msg: string): void {
    this.actionMessage.set(msg);
    setTimeout(() => this.actionMessage.set(null), 4000);
  }
}

