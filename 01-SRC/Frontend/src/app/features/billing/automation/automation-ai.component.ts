import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  EnterpriseApiService,
  AutomationRule,
  NotificationItem,
  AiAssistantResponse,
} from '../../../core/services/enterprise-api.service';

@Component({
  selector: 'magic-automation-ai',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './automation-ai.component.html',
  styleUrl: './automation-ai.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutomationAiComponent implements OnInit {
  private readonly api = inject(EnterpriseApiService);
  private readonly router = inject(Router);

  protected readonly activeTab = signal<'ai-assistant' | 'rules' | 'notifications'>('ai-assistant');
  protected readonly rules = signal<readonly AutomationRule[]>([]);
  protected readonly notifications = signal<readonly NotificationItem[]>([]);
  protected readonly aiResponse = signal<AiAssistantResponse | null>(null);
  protected readonly loading = signal(false);
  protected readonly askingAi = signal(false);
  protected readonly actionMessage = signal<string | null>(null);

  // Modals
  protected readonly showRuleModal = signal(false);

  // AI Prompt Input
  protected naturalLanguagePrompt = '';

  // New Rule Form State
  protected ruleForm = {
    name: '',
    triggerType: 'LowStockDetected',
    conditionJson: '{"threshold": 10}',
    actionType: 'CreateNotification',
    actionPayloadJson: '{"priority": "High", "channel": "InApp"}',
  };

  ngOnInit(): void {
    this.loadNotifications();
    this.loadRules();
  }

  protected setTab(tab: 'ai-assistant' | 'rules' | 'notifications'): void {
    this.activeTab.set(tab);
    if (tab === 'rules') this.loadRules();
    if (tab === 'notifications') this.loadNotifications();
  }

  protected loadRules(): void {
    this.loading.set(true);
    this.api.getAutomationRules().subscribe({
      next: (res) => {
        this.rules.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadNotifications(): void {
    this.api.getNotifications().subscribe({
      next: (res) => this.notifications.set(res),
      error: () => {},
    });
  }

  protected askAi(promptText?: string): void {
    const query = promptText || this.naturalLanguagePrompt;
    if (!query.trim()) return;

    this.askingAi.set(true);
    this.api.askAiAssistant(query).subscribe({
      next: (res) => {
        this.aiResponse.set(res);
        this.askingAi.set(false);
      },
      error: (err) => {
        this.askingAi.set(false);
        this.notify(`AI Assistant query failed: ${err.message}`);
      },
    });
  }

  protected navigateToDrilldown(url?: string): void {
    if (url) {
      this.router.navigateByUrl(url);
    }
  }

  protected createRule(): void {
    this.api.createAutomationRule(this.ruleForm).subscribe({
      next: () => {
        this.showRuleModal.set(false);
        this.notify('Automation rule configured successfully');
        this.loadRules();
      },
      error: (err) => this.notify(`Failed to create rule: ${err.message}`),
    });
  }

  protected executeRule(ruleId: string): void {
    this.api.triggerRuleEvaluation(ruleId).subscribe({
      next: () => {
        this.notify('Rule evaluation triggered successfully');
        this.loadRules();
        this.loadNotifications();
      },
      error: (err) => this.notify(`Execution failed: ${err.message}`),
    });
  }

  protected markNotificationRead(id: string): void {
    this.api.markNotificationAsRead(id).subscribe({
      next: () => this.loadNotifications(),
      error: () => {},
    });
  }

  private notify(msg: string): void {
    this.actionMessage.set(msg);
    setTimeout(() => this.actionMessage.set(null), 4000);
  }
}

