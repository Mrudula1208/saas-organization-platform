import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportService, AdminReportData, QuarterlyStat, MonthlyStat } from '../../../core/services/report';

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
    alert(`System Reports Export sequence initialized! Your file is being compiled into ${format} format and will download shortly.`);
  }
}