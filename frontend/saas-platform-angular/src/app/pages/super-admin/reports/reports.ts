import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportService } from '../../../core/services/report';
import { AdminReportData, QuarterlyStat, MonthlyStat } from '../../../models/report.model';

interface GrowthRecord {
  label: string;
  count: number;
  heightPercent: number;
}

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './reports.html',
  styleUrl: './reports.css',
})
export class Reports implements OnInit {
  tenantGrowth: GrowthRecord[] = [];
  userGrowth: GrowthRecord[] = [];

  avgTenantLifetime = 0;
  customerAcquisitionCost = 0;
  churnRate = 0;
  totalTenants = 0;
  totalUsers = 0;

  isLoading = true;
  errorMessage = '';

  exportingFormat: 'PDF' | 'Excel' | null = null;
  exportError = '';

  private monthNames = ['JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC'];

  constructor(private reportService: ReportService) {}

  ngOnInit() {
    this.loadReport();
  }

  loadReport() {
    this.isLoading = true;
    this.errorMessage = '';

    this.reportService.getAdminReport().subscribe({
      next: (data: AdminReportData) => {
        this.tenantGrowth = this.buildTenantGrowthChart(data.quarterlyTenants);
        this.userGrowth = this.buildUserGrowthChart(data.monthlyUsers);
        this.avgTenantLifetime = data.avgLifetimeMonths;
        this.customerAcquisitionCost = data.customerAcquisitionCost;
        this.churnRate = data.churnRate;
        this.totalTenants = data.totalTenants;
        this.totalUsers = data.totalUsers;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Could not load global reports. Please try again later.';
      },
    });
  }

  private buildTenantGrowthChart(quarterlyData: QuarterlyStat[]): GrowthRecord[] {
    if (!quarterlyData || quarterlyData.length === 0) {
      return [];
    }
    const maxCount = Math.max(...quarterlyData.map(q => q.count), 1);
    return quarterlyData.map(q => ({
      label: 'Q' + q.quarter + ' ' + q.year,
      count: q.count,
      heightPercent: Math.round((q.count / maxCount) * 100)
    }));
  }

  private buildUserGrowthChart(monthlyData: MonthlyStat[]): GrowthRecord[] {
    if (!monthlyData || monthlyData.length === 0) {
      return [];
    }
    const maxCount = Math.max(...monthlyData.map(m => m.count), 1);
    return monthlyData.map(m => ({
      label: this.monthNames[m.month - 1] || '???',
      count: m.count,
      heightPercent: Math.round((m.count / maxCount) * 100)
    }));
  }

  exportData(format: 'PDF' | 'Excel') {
    if (this.exportingFormat) {
      return;
    }
    this.exportError = '';
    this.exportingFormat = format;

    const request =
      format === 'PDF'
        ? this.reportService.exportAdminReportPdf()
        : this.reportService.exportAdminReportExcel();

    request.subscribe({
      next: ({ blob, fileName }) => {
        this.exportingFormat = null;
        this.downloadBlob(blob, fileName);
      },
      error: async (err) => {
        this.exportingFormat = null;
        this.exportError = await this.readExportError(err, format);
      },
    });
  }

  private async readExportError(err: any, format: string): Promise<string> {
    const fallback = `Could not generate the ${format} report. Please try again.`;
    try {
      if (err?.error instanceof Blob) {
        const parsed = JSON.parse(await err.error.text());
        if (parsed?.message) {
          return parsed.message;
        }
      }
      if (err?.error?.message) {
        return err.error.message;
      }
    } catch {
      // Response was not JSON — use the generic message.
    }
    return fallback;
  }

  private downloadBlob(blob: Blob, fileName: string) {
    if (typeof document === 'undefined') {
      return;
    }
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}