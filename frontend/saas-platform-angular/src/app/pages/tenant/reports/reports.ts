import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

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
  projectsCreated: ReportStat[] = [
    { month: 'DEC', count: 1, heightPercent: 20 },
    { month: 'JAN', count: 2, heightPercent: 40 },
    { month: 'FEB', count: 2, heightPercent: 40 },
    { month: 'MAR', count: 3, heightPercent: 60 },
    { month: 'APR', count: 4, heightPercent: 80 },
    { month: 'MAY', count: 5, heightPercent: 100 }
  ];

  tasksCompleted: ReportStat[] = [
    { month: 'DEC', count: 8, heightPercent: 25 },
    { month: 'JAN', count: 12, heightPercent: 37 },
    { month: 'FEB', count: 18, heightPercent: 56 },
    { month: 'MAR', count: 22, heightPercent: 68 },
    { month: 'APR', count: 27, heightPercent: 84 },
    { month: 'MAY', count: 32, heightPercent: 100 }
  ];

  ngOnInit() {
    // Loaded tenant specific productivity insights
  }

  exportReport(format: 'PDF' | 'Excel') {
    alert(`Tenant Workspace Analytics compiled! Preparing ${format} download file. It will download in a few seconds.`);
  }
}
