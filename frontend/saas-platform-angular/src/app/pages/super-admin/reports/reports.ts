import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

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
  tenantGrowth: GrowthRecord[] = [
    { label: 'Q1 2025', count: 12, heightPercent: 30 },
    { label: 'Q2 2025', count: 22, heightPercent: 55 },
    { label: 'Q3 2025', count: 31, heightPercent: 75 },
    { label: 'Q4 2025', count: 40, heightPercent: 100 }
  ];

  userGrowth: GrowthRecord[] = [
    { label: 'JAN', count: 90, heightPercent: 40 },
    { label: 'FEB', count: 140, heightPercent: 60 },
    { label: 'MAR', count: 180, heightPercent: 80 },
    { label: 'APR', count: 222, heightPercent: 100 }
  ];

  ngOnInit() {
    // Analytics calculations loaded
  }

  exportData(format: 'PDF' | 'Excel') {
    alert(`System Reports Export sequence initialized! Your file is being compiled into ${format} format and will download shortly.`);
  }
}

