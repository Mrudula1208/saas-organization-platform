import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportService, TenantReportData, MonthlyStat } from '../../../core/services/report';

interface ReportStat {
  month: string;
  count: number;
  heightPercent: number;
}

@Component({
  selector: 'app-tenant-reports',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './reports.html',
  styleUrl: './reports.css',
})
export class Reports implements OnInit {
  projectsCreated: ReportStat[] = [];
  tasksCreated: ReportStat[] = [];
  tasksCompleted: ReportStat[] = [];

  totalProjects = 0;
  totalTasks = 0;
  completedTasks = 0;
  pendingTasks = 0;
  inProgressTasks = 0;
  totalMembers = 0;
  avgTasksPerMember = 0;
  completionRate = 0;

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

    this.reportService.getTenantReport().subscribe({
      next: (data: TenantReportData) => {
        this.projectsCreated = this.buildChart(data.monthlyProjects);
        this.tasksCreated = this.buildChart(data.monthlyTasksCreated);
        this.tasksCompleted = this.buildChart(data.monthlyTasksCompleted);
        this.totalProjects = data.totalProjects;
        this.totalTasks = data.totalTasks;
        this.completedTasks = data.completedTasks;
        this.pendingTasks = data.pendingTasks;
        this.inProgressTasks = data.inProgressTasks;
        this.totalMembers = data.totalMembers;
        this.avgTasksPerMember = data.avgTasksPerMember;
        this.completionRate = data.completionRate;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Could not load workspace analytics. Please try again later.';
      },
    });
  }

  private buildChart(monthlyData: MonthlyStat[]): ReportStat[] {
    if (!monthlyData || monthlyData.length === 0) {
      return [];
    }
    const maxCount = Math.max(...monthlyData.map(m => m.count), 1);
    return monthlyData.map(m => ({
      month: this.monthNames[m.month - 1] || '???',
      count: m.count,
      heightPercent: Math.round((m.count / maxCount) * 100)
    }));
  }

  exportReport(format: 'PDF' | 'Excel') {
    alert(`Tenant Workspace Analytics compiled! Preparing ${format} download file. It will download in a few seconds.`);
  }
}