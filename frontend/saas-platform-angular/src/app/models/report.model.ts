export interface MonthlyStat {
  year: number;
  month: number;
  count: number;
}

export interface QuarterlyStat {
  year: number;
  quarter: number;
  count: number;
}

export interface TenantReportData {
  monthlyProjects: MonthlyStat[];
  monthlyTasksCreated: MonthlyStat[];
  monthlyTasksCompleted: MonthlyStat[];
  totalProjects: number;
  totalTasks: number;
  completedTasks: number;
  pendingTasks: number;
  inProgressTasks: number;
  totalMembers: number;
  avgTasksPerMember: number;
  completionRate: number;
}

export interface AdminReportData {
  quarterlyTenants: QuarterlyStat[];
  monthlyUsers: MonthlyStat[];
  totalTenants: number;
  totalUsers: number;
  avgLifetimeMonths: number;
  customerAcquisitionCost: number;
  churnRate: number;
}