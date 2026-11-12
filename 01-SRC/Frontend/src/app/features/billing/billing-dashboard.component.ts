import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'magic-billing-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './billing-dashboard.component.html',
  styleUrl: './billing-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BillingDashboardComponent {
  protected readonly plannedCapabilities = [
    {
      title: 'POS Terminal & Express Checkout',
      tag: 'Retail Operations',
      icon: '💳',
      description: 'High-speed barcode scanning, keyboard-first cashier interface, touch optimization, multi-tender split payment, and ESC/POS thermal printing.',
    },
    {
      title: 'Tax Invoicing & Sales Management',
      tag: 'B2B & Wholesale',
      icon: '🧾',
      description: 'Statutory GST tax invoices, credit sales, customer credit limit enforcement, quotation-to-invoice conversion, and delivery challans.',
    },
    {
      title: 'Inventory & Stock Control',
      tag: 'Warehouse',
      icon: '📦',
      description: 'Real-time stock valuation, reorder thresholds, batch/expiry tracking, serial numbers, and inter-branch inventory transfers.',
    },
    {
      title: 'Procurement & Vendor AP',
      tag: 'Supply Chain',
      icon: '🛒',
      description: 'Purchase orders, Goods Receipt Notes (GRN), vendor bill matching, price list management, and return to vendor.',
    },
    {
      title: 'Customer Subledger Integration',
      tag: 'Accounting Sync',
      icon: '👥',
      description: 'Automatic real-time ledger postings to GL control accounts (11300 Trade Debtors, 21100 Trade Creditors) without duplicate data entry.',
    },
    {
      title: 'Cashier Day Closing & Reconciliation',
      tag: 'Governance',
      icon: '🔒',
      description: 'Shift start float tracking, mid-day cash drops, register closing count, cash variance analysis, and auto-journal voucher posting.',
    },
  ];
}
