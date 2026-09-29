import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ReportService } from '../../../core/services/report';
import { ProjectService } from '../../../core/services/project';
import { Auth } from '../../../core/services/auth';
import { triggerServerDownload } from '../../../core/helpers';
import { TenantReportData } from '../../../models/report.model';
import { Project } from '../../../models/project.model';

export interface MonthlyStackedBar {
  month: string;
  complete: number;
  active: number;
  pending: number;
  total: number;
  completePercent: number;
  activePercent: number;
  pendingPercent: number;
}

export interface ReportRow {
  id: string;
  name: string;
  avatarText: string;
  subText: string;
  stating: string;
  lastUsed: string;
  monthlyRevenue: string;
  status: string;
  statusType: 'warning' | 'info' | 'success';
  selected?: boolean;
}

export interface ActivityItem {
  id: number;
  title: string;
  subtitle: string;
  timeAgo: string;
  icon: string;
  iconBg: string;
  iconColor: string;
}

@Component({
  selector: 'app-tenant-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './reports.html',
  styleUrl: './reports.css',
})
export class Reports implements OnInit {
  // Top breadcrumb & filters
  dateRangeStart = '2023-06-13';
  dateRangeEnd = '2023-08-13';
  dateRangeDisplay = 'Jun 13, 2023 → Aug 13, 2023';
  showDatePicker = false;

  selectedProjectId = '';
  projectsList: Project[] = [];

  // Export state
  exportingFormat: 'PDF' | 'Excel' | null = null;
  exportError = '';
  exportSuccessToast = '';
  errorMessage = '';
  isLoading = true;

  // Chart data
  monthlyStackedBars: MonthlyStackedBar[] = [];
  productivityScore = 50;
  completionRate = 78;

  // Radial Gauges (Circumference calculation for r = 45 -> c = 282.7, semicircle = 141.4)
  readonly gaugeCircumference = 141.4;

  get productivityStrokeDashoffset(): number {
    const fraction = Math.min(Math.max(this.productivityScore / 100, 0), 1);
    return this.gaugeCircumference * (1 - fraction);
  }

  get completionStrokeDashoffset(): number {
    const fraction = Math.min(Math.max(this.completionRate / 100, 0), 1);
    return this.gaugeCircumference * (1 - fraction);
  }

  // Reports Table
  reportsTableData: ReportRow[] = [
    {
      id: 'rep-1',
      name: 'Acme Corp',
      avatarText: 'AC',
      subText: 'Tenant Created',
      stating: 'Production Staged',
      lastUsed: '1 month ago',
      monthlyRevenue: '$345,100',
      status: 'Awaiting Review',
      statusType: 'warning',
      selected: false
    },
    {
      id: 'rep-2',
      name: 'Acme Corp',
      avatarText: 'AC',
      subText: 'Design System Delivery',
      stating: 'Tier Upgraded',
      lastUsed: '7 days ago',
      monthlyRevenue: '$345,100',
      status: 'Upgraded',
      statusType: 'info',
      selected: false
    },
    {
      id: 'rep-3',
      name: 'Global UI Module',
      avatarText: 'GM',
      subText: 'Sprint Deliverable',
      stating: 'Production Staged',
      lastUsed: '2 weeks ago',
      monthlyRevenue: '$180,400',
      status: 'Upgraded',
      statusType: 'info',
      selected: false
    },
    {
      id: 'rep-4',
      name: 'Enterprise Sync Engine',
      avatarText: 'ES',
      subText: 'Worker Daemon',
      stating: 'Core Migration',
      lastUsed: '3 days ago',
      monthlyRevenue: '$420,000',
      status: 'Active',
      statusType: 'success',
      selected: false
    },
    {
      id: 'rep-5',
      name: 'Mobile Client Gateway',
      avatarText: 'MG',
      subText: 'GraphQL Adapter',
      stating: 'Security Verified',
      lastUsed: 'Yesterday',
      monthlyRevenue: '$95,500',
      status: 'Active',
      statusType: 'success',
      selected: false
    }
  ];

  selectAll = false;
  currentPage = 1;
  totalPages = 3;

  // Right Column: Recent Activity Feed
  activityFeed: ActivityItem[] = [
    {
      id: 1,
      title: 'Acme Corp Tenant Created',
      subtitle: 'Acme Corp Tenant Created',
      timeAgo: '2 months ago',
      icon: 'business',
      iconBg: 'rgba(37, 99, 235, 0.15)',
      iconColor: '#2563EB'
    },
    {
      id: 2,
      title: 'User registered',
      subtitle: 'User registered at K586385',
      timeAgo: '2 months ago',
      icon: 'person',
      iconBg: 'rgba(59, 130, 246, 0.15)',
      iconColor: '#3B82F6'
    },
    {
      id: 3,
      title: 'Plan upgraded',
      subtitle: 'Acme upgraded at $375,100',
      timeAgo: '2 months ago',
      icon: 'trending_up',
      iconBg: 'rgba(168, 85, 247, 0.15)',
      iconColor: '#A855F7'
    },
    {
      id: 4,
      title: 'Plan upgraded',
      subtitle: 'User registered at $385931',
      timeAgo: '2 months ago',
      icon: 'verified',
      iconBg: 'rgba(16, 185, 129, 0.15)',
      iconColor: '#10B981'
    },
    {
      id: 5,
      title: 'Plan upgraded',
      subtitle: 'Acme Corp Tenant Created',
      timeAgo: '2 months ago',
      icon: 'rocket_launch',
      iconBg: 'rgba(59, 130, 246, 0.15)',
      iconColor: '#3B82F6'
    },
    {
      id: 6,
      title: 'Plan upgraded',
      subtitle: 'Acme Corp Tenant Created',
      timeAgo: '2 months ago',
      icon: 'workspace_premium',
      iconBg: 'rgba(37, 99, 235, 0.15)',
      iconColor: '#2563EB'
    }
  ];

  constructor(
    private reportService: ReportService,
    private projectService: ProjectService,
    private auth: Auth,
    private cdr: ChangeDetectorRef
  ) {}

  get roleLabel(): string {
    const role = this.auth.currentUser()?.role;
    if (role === 'Member') return 'Tenant Member';
    if (role === 'Manager') return 'Tenant Manager';
    if (role === 'TenantAdmin') return 'Tenant Admin';
    return role || 'Tenant Admin';
  }

  ngOnInit() {
    this.loadProjects();
    this.loadReport();
  }

  loadProjects() {
    this.projectService.getProjects(1, 50).subscribe({
      next: (res) => {
        this.projectsList = res.data || [];
        if (this.projectsList.length > 0) {
          this.reportsTableData = this.projectsList.map((p, idx) => ({
            id: p.id,
            name: p.name,
            avatarText: (p.name || 'PR').substring(0, 2).toUpperCase(),
            subText: p.description || 'Active Workspace',
            stating: p.status || 'Active',
            lastUsed: 'Recently',
            monthlyRevenue: '$' + ((idx + 1) * 45000).toLocaleString(),
            status: p.status || 'Active',
            statusType: 'success' as const,
            selected: false
          }));
        }
        this.cdr.markForCheck();
      },
      error: () => {
        // Fallback gracefully
      }
    });
  }

  loadReport() {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    this.reportService.getTenantReport().subscribe({
      next: (data: TenantReportData) => {
        this.buildStackedChart(data);
        if (data.completionRate !== undefined && data.completionRate > 0) {
          this.completionRate = Math.round(data.completionRate);
        } else {
          this.completionRate = 78;
        }

        if (data.avgTasksPerMember && data.avgTasksPerMember > 0) {
          this.productivityScore = Math.min(Math.round(data.avgTasksPerMember * 10), 100);
        } else {
          this.productivityScore = 50;
        }

        this.isLoading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: () => {
        this.buildDefaultStackedChart();
        this.completionRate = 78;
        this.productivityScore = 50;
        this.isLoading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  private buildStackedChart(data: TenantReportData) {
    const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'May'];
    const mockBaseline = [
      { complete: 200, active: 150, pending: 100 },
      { complete: 380, active: 250, pending: 140 },
      { complete: 520, active: 280, pending: 160 },
      { complete: 700, active: 310, pending: 190 },
      { complete: 780, active: 320, pending: 200 },
      { complete: 620, active: 300, pending: 180 },
      { complete: 790, active: 330, pending: 210 },
      { complete: 680, active: 290, pending: 190 },
      { complete: 850, active: 350, pending: 220 }
    ];

    const maxChartValue = 1420;

    this.monthlyStackedBars = monthNames.map((month, idx) => {
      const base = mockBaseline[idx % mockBaseline.length];
      const total = base.complete + base.active + base.pending;

      return {
        month,
        complete: base.complete,
        active: base.active,
        pending: base.pending,
        total,
        completePercent: Math.round((base.complete / maxChartValue) * 100),
        activePercent: Math.round((base.active / maxChartValue) * 100),
        pendingPercent: Math.round((base.pending / maxChartValue) * 100)
      };
    });
  }

  private buildDefaultStackedChart() {
    this.buildStackedChart({} as any);
  }

  onProjectFilterChange() {
    // Re-filter or update reports
    if (this.selectedProjectId) {
      const selected = this.projectsList.find(p => p.id === this.selectedProjectId);
      if (selected) {
        this.reportsTableData = [
          {
            id: selected.id,
            name: selected.name,
            avatarText: selected.name.slice(0, 2).toUpperCase(),
            subText: selected.priority + ' Priority',
            stating: selected.description ? selected.description.slice(0, 25) + '...' : 'In Progress',
            lastUsed: 'Active Now',
            monthlyRevenue: '$' + (Math.round(selected.progress * 1500 + 45000)).toLocaleString(),
            status: selected.status || 'Active',
            statusType: selected.status === 'Completed' ? 'success' : 'info',
            selected: false
          }
        ];
        return;
      }
    }
    // Reset to default sample rows
    this.loadReport();
  }

  applyDateRange(start: string, end: string) {
    this.dateRangeStart = start;
    this.dateRangeEnd = end;
    const sDate = new Date(start);
    const eDate = new Date(end);
    const options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric', year: 'numeric' };
    this.dateRangeDisplay = `${sDate.toLocaleDateString('en-US', options)} → ${eDate.toLocaleDateString('en-US', options)}`;
    this.showDatePicker = false;
  }

  toggleDatePicker() {
    this.showDatePicker = !this.showDatePicker;
  }

  toggleAllSelection() {
    this.selectAll = !this.selectAll;
    this.reportsTableData.forEach(item => item.selected = this.selectAll);
  }

  checkSingleSelection() {
    this.selectAll = this.reportsTableData.length > 0 && this.reportsTableData.every(i => i.selected);
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  get pdfExportUrl(): string {
    return this.reportService.getTenantExportUrl('PDF');
  }

  get excelExportUrl(): string {
    return this.reportService.getTenantExportUrl('Excel');
  }

  onExportClick(format: 'PDF' | 'Excel') {
    const extLabel = format === 'PDF' ? 'PDF report (.pdf)' : 'Excel spreadsheet (.xlsx)';
    this.showExportToast(`Downloading ${extLabel}...`);
  }

  exportReport(format: 'PDF' | 'Excel') {
    if (this.exportingFormat) return;

    this.exportError = '';
    this.exportSuccessToast = '';
    this.exportingFormat = format;

    try {
      const downloadUrl = format === 'PDF' ? this.pdfExportUrl : this.excelExportUrl;
      const fileName = format === 'PDF' ? 'workspace-analytics.pdf' : 'workspace-analytics.xlsx';
      triggerServerDownload(downloadUrl, fileName);
      const extLabel = format === 'PDF' ? 'PDF report (.pdf)' : 'Excel spreadsheet (.xlsx)';
      this.showExportToast(`Downloading ${extLabel}...`);
    } catch {
      this.exportError = `Could not initiate ${format} export. Please try again.`;
    } finally {
      setTimeout(() => {
        this.exportingFormat = null;
        this.cdr.markForCheck();
      }, 1500);
    }
  }

  private showExportToast(msg: string) {
    this.exportSuccessToast = msg;
    setTimeout(() => {
      this.exportSuccessToast = '';
      this.cdr.markForCheck();
    }, 4500);
  }

  private async readExportError(err: any, format: string): Promise<string> {
    const fallback = `Could not generate the ${format} report. Please try again.`;
    try {
      if (err?.error instanceof Blob) {
        const parsed = JSON.parse(await err.error.text());
        if (parsed?.message) return parsed.message;
      }
      if (err?.error?.message) return err.error.message;
    } catch {
      // Ignored
    }
    return fallback;
  }
}