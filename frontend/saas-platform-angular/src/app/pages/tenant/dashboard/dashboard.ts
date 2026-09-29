import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ProjectService } from '../../../core/services/project';
import { UserService } from '../../../core/services/user';
import { SystemLogService } from '../../../core/services/system-log';
import { BillingService } from '../../../core/services/billing';
import { TenantService } from '../../../core/services/tenant';
import { NotificationService } from '../../../core/services/notification';
import { Auth } from '../../../core/services/auth';
import { Project } from '../../../models/project.model';
import { TaskItem } from '../../../models/task.model';
import { CurrentPlan } from '../../../models/payment.model';
import { SystemLog } from '../../../models/system-log.model';
import { AppNotification } from '../../../models/notification.model';

export interface SprintTaskCard {
  id: string;
  name: string;
  taskTag: string;
  status: 'Backlog' | 'In Progress' | 'Review' | 'Done';
}

export interface ActivityFeedItem {
  id: string;
  icon: string;
  iconColor: string;
  iconBg: string;
  title: string;
  subtitle: string;
  timeAgo: string;
}

export interface ProjectTableRow {
  id: string;
  name: string;
  subtitle: string;
  initials: string;
  priority: string;
  priorityClass: string;
  progressPercent: number;
  completedTasksCount: number;
  totalTasksCount: number;
  lastUpdated: string;
  status: string;
  statusDotClass: string;
  isSelected?: boolean;
}

export interface DualBarItem {
  month: string;
  cyanHeight: number;
  orangeHeight: number;
  cyanY: number;
  orangeY: number;
  x: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  // Top breadcrumb / tenant metadata
  tenantName = 'TechNova Solutions';

  // Top 4 KPI Metrics
  totalUsers = 5;
  totalUsersTrend = 'Active team members';
  totalProjects = 3;
  totalProjectsTrend = 'Active workspaces';
  activeTasksCount = 8;
  activeTasksTrend = 'In progress & review';
  completedTasksCount = 4;
  completedTasksTrend = '33% completion rate';

  // Role Scoping Metrics (Member & Manager)
  myAssignedTasksCount = 0;
  myCompletedTasksCount = 0;
  myInProgressTasksCount = 0;

  get roleLabel(): string {
    const role = this.auth.currentUser()?.role;
    if (role === 'Member') return 'Tenant Member';
    if (role === 'Manager') return 'Tenant Manager';
    if (role === 'TenantAdmin') return 'Tenant Admin';
    return role || 'Tenant Member';
  }

  get userRole(): string {
    return this.auth.currentUser()?.role || 'Member';
  }

  get isMember(): boolean {
    return this.userRole === 'Member';
  }

  get isManager(): boolean {
    return this.userRole === 'Manager';
  }

  get isTenantAdmin(): boolean {
    return this.userRole === 'TenantAdmin' || this.userRole === 'SuperAdmin';
  }

  get canCreateProject(): boolean {
    return this.auth.hasRole(['SuperAdmin', 'TenantAdmin', 'Manager']);
  }

  get currentUserId(): string | null {
    return this.auth.currentUser()?.id || null;
  }

  // 2. Middle Left: Project Overviews 4-Lane Sprint Board
  backlogTasks: SprintTaskCard[] = [];
  inProgressTasks: SprintTaskCard[] = [];
  reviewTasks: SprintTaskCard[] = [];
  doneTasks: SprintTaskCard[] = [];

  // Dropdown filter state
  selectedInteractive = 'Interactive';
  interactiveDropdownOpen = false;
  interactiveOptions = ['Interactive', 'Weekly Milestones', 'Deliverables Only', 'All Tasks'];

  // 3. Middle Right: Tasks Completion Breakdown & Progress
  completionRate = 0;
  inProgressTasksCount = 0;
  inProgressRate = 0;
  reviewTasksCount = 0;
  reviewRate = 0;
  backlogTasksCount = 0;
  backlogRate = 0;
  donutDashOffset = 339.29;
  dualBars: DualBarItem[] = [];

  // 4. Right Column: Recent Activity Feed
  recentActivities: ActivityFeedItem[] = [];

  // 5. Bottom Table: Organization Projects Workspace
  tableRows: ProjectTableRow[] = [];
  selectAll = false;

  // Subscription plan & quotas
  currentPlan: CurrentPlan | null = null;
  isLoading = true;

  constructor(
    private projectService: ProjectService,
    private userService: UserService,
    private systemLogService: SystemLogService,
    private billingService: BillingService,
    private tenantService: TenantService,
    private notifService: NotificationService,
    public auth: Auth,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadTenantDashboard();
  }

  loadTenantDashboard() {
    this.isLoading = true;
    this.cdr.markForCheck();

    forkJoin({
      plan: this.billingService.getCurrentPlan().pipe(catchError(() => of(null))),
      projectsRes: this.projectService.getProjects(1, 200).pipe(catchError(() => of({ data: [], totalCount: 0, page: 1, pageSize: 200 }))),
      tasksRes: this.projectService.getTasks(1, 200).pipe(catchError(() => of({ data: [], totalCount: 0, page: 1, pageSize: 200 }))),
      usersRes: this.userService.getUsers(1, 200).pipe(catchError(() => of({ data: [], totalCount: 0, page: 1, pageSize: 200 } as any))),
      logsRes: this.systemLogService.getLogs('', '', '', '', 1, 15).pipe(catchError(() => of({ data: [], totalCount: 0, page: 1, pageSize: 15 }))),
      notifsRes: this.notifService.getNotifications().pipe(catchError(() => of([]))),
      settingsRes: this.tenantService.getSettings().pipe(catchError(() => of(null)))
    }).subscribe({
      next: ({ plan, projectsRes, tasksRes, usersRes, logsRes, notifsRes, settingsRes }) => {
        try {
          this.currentPlan = plan;

          if (settingsRes?.name) {
            this.tenantName = settingsRes.name;
          }

          const rawProjects: Project[] = Array.isArray(projectsRes?.data) ? projectsRes.data : [];
          const rawTasks: TaskItem[] = Array.isArray(tasksRes?.data) ? tasksRes.data : [];
          const rawUsers: any[] = Array.isArray((usersRes as any)?.data) ? (usersRes as any).data : (Array.isArray(usersRes) ? usersRes : []);
          const rawLogs: SystemLog[] = Array.isArray(logsRes?.data) ? logsRes.data : [];
          const rawNotifs: AppNotification[] = Array.isArray(notifsRes) ? notifsRes : [];

          const uid = this.currentUserId;

          // Scope tasks according to role
          const effectiveTasks = (this.isMember && uid)
            ? rawTasks.filter(t => t.assignedUserId === uid)
            : rawTasks;

          const memberProjectIds = new Set(effectiveTasks.map(t => t.projectId));
          const effectiveProjects = (this.isMember && uid && memberProjectIds.size > 0)
            ? rawProjects.filter(p => memberProjectIds.has(p.id))
            : rawProjects;

          // 1. Top 4 KPI Metrics - Accurate Real Data
          this.totalUsers = rawUsers.length;
          this.totalProjects = effectiveProjects.length;

          const myCompleted = effectiveTasks.filter(t => t.status === 'Completed' || t.isCompleted);
          const myInProgress = effectiveTasks.filter(t => t.status === 'In Progress');
          const myReview = effectiveTasks.filter(t => t.status === 'Pending' || t.status === 'Review');
          const myBacklog = effectiveTasks.filter(t => t.status === 'To Do' || t.status === 'Backlog');
          const myActive = effectiveTasks.filter(t => t.status !== 'Completed' && !t.isCompleted);

          this.myAssignedTasksCount = effectiveTasks.length;
          this.myCompletedTasksCount = myCompleted.length;
          this.myInProgressTasksCount = myInProgress.length;

          this.completedTasksCount = myCompleted.length;
          this.inProgressTasksCount = myInProgress.length;
          this.activeTasksCount = myActive.length;

          const totalTaskCount = effectiveTasks.length;
          this.completionRate = totalTaskCount > 0 ? Math.round((myCompleted.length / totalTaskCount) * 100) : 0;
          this.inProgressRate = totalTaskCount > 0 ? Math.round((myInProgress.length / totalTaskCount) * 100) : 0;
          this.reviewTasksCount = myReview.length;
          this.reviewRate = totalTaskCount > 0 ? Math.round((myReview.length / totalTaskCount) * 100) : 0;
          this.backlogTasksCount = myBacklog.length;
          this.backlogRate = totalTaskCount > 0 ? Math.max(0, 100 - (this.completionRate + this.inProgressRate + this.reviewRate)) : 0;

          const circumference = 339.29;
          this.donutDashOffset = circumference * (1 - this.completionRate / 100);

          // 2. Project Overviews 4-Lane Sprint Board
          this.populateKanbanLanes(effectiveTasks);

          // 3. Activity Feed: User joined project, Task completed, Project created, User logged in
          this.buildRecentActivities(rawLogs, rawNotifs);

          // 4. Monthly Delivery Velocity chart
          this.buildDualBarChart(effectiveTasks);

          // 5. Bottom Table: Organization Projects
          this.populateTableRows(effectiveProjects, effectiveTasks);
        } catch (e) {
          console.error('Error computing tenant dashboard data:', e);
        } finally {
          this.isLoading = false;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        }
      },
      error: () => {
        this.isLoading = false;
        this.buildRecentActivities([]);
        this.buildDualBarChart([]);
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  private populateKanbanLanes(tasks: TaskItem[]) {
    this.backlogTasks = tasks
      .filter(t => t.status === 'To Do' || t.status === 'Backlog')
      .map((t, idx) => ({ id: t.id, name: t.name, taskTag: `Tasks #${idx + 1}`, status: 'Backlog' as const }));

    this.inProgressTasks = tasks
      .filter(t => t.status === 'In Progress')
      .map((t, idx) => ({ id: t.id, name: t.name, taskTag: `Tasks #${idx + 1}`, status: 'In Progress' as const }));

    this.reviewTasks = tasks
      .filter(t => t.status === 'Pending' || t.status === 'Review')
      .map((t, idx) => ({ id: t.id, name: t.name, taskTag: `Tasks #${idx + 1}`, status: 'Review' as const }));

    this.doneTasks = tasks
      .filter(t => t.status === 'Completed' || t.isCompleted)
      .map((t, idx) => ({ id: t.id, name: t.name, taskTag: `Tasks #${idx + 1}`, status: 'Done' as const }));
  }

  private buildDualBarChart(tasks: TaskItem[] = []) {
    // 5 months task delivery velocity calculated from real database tasks
    const chartHeight = 70;
    const now = new Date();
    const months: { month: string; monthIdx: number; year: number }[] = [];
    for (let i = 4; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      months.push({
        month: d.toLocaleString('en-US', { month: 'short' }),
        monthIdx: d.getMonth(),
        year: d.getFullYear()
      });
    }

    const baseScales = months.map(m => {
      const createdCount = tasks.filter(t => {
        if (!t.createdAt) return false;
        const cd = new Date(t.createdAt);
        return cd.getMonth() === m.monthIdx && cd.getFullYear() === m.year;
      }).length;

      const completedCount = tasks.filter(t => {
        if (!t.isCompleted && t.status !== 'Completed') return false;
        const cd = t.completedAt ? new Date(t.completedAt) : (t.createdAt ? new Date(t.createdAt) : null);
        return cd ? (cd.getMonth() === m.monthIdx && cd.getFullYear() === m.year) : false;
      }).length;

      return {
        month: m.month,
        createdCount,
        completedCount
      };
    });

    const maxCount = Math.max(1, ...baseScales.map(s => Math.max(s.createdCount, s.completedCount)));
    const maxScale = Math.max(5, Math.ceil(maxCount / 5) * 5);

    this.dualBars = baseScales.map((item, idx) => {
      const cyanH = item.createdCount > 0 ? Math.max(4, Math.round((item.createdCount / maxScale) * chartHeight)) : 0;
      const orangeH = item.completedCount > 0 ? Math.max(4, Math.round((item.completedCount / maxScale) * chartHeight)) : 0;
      return {
        month: item.month,
        cyanHeight: cyanH,
        orangeHeight: orangeH,
        cyanY: 80 - cyanH,
        orangeY: 80 - orangeH,
        x: 42 + idx * 48
      };
    });
  }

  private buildRecentActivities(rawLogs: SystemLog[], notifications: AppNotification[] = []) {
    // If user is Member and has notifications, display real member notifications
    if (this.isMember && notifications.length > 0) {
      this.recentActivities = notifications.slice(0, 5).map((n, idx) => ({
        id: `notif-${idx}`,
        icon: 'notifications',
        iconColor: '#3B82F6',
        iconBg: 'rgba(59, 130, 246, 0.15)',
        title: n.message,
        subtitle: 'Activity notification',
        timeAgo: n.createdAt ? this.formatRelativeTime(n.createdAt) : 'Recently'
      }));
      return;
    }

    if (rawLogs.length === 0) {
      if (notifications.length > 0) {
        this.recentActivities = notifications.slice(0, 5).map((n, idx) => ({
          id: `notif-${idx}`,
          icon: 'notifications',
          iconColor: '#3B82F6',
          iconBg: 'rgba(59, 130, 246, 0.15)',
          title: n.message,
          subtitle: 'Workspace notification',
          timeAgo: n.createdAt ? this.formatRelativeTime(n.createdAt) : 'Recently'
        }));
      } else {
        this.recentActivities = [];
      }
      return;
    }

    this.recentActivities = rawLogs.slice(0, 5).map(log => {
      const act = (log.action || '').toUpperCase();
      let icon = 'notifications';
      let iconColor = '#3B82F6';
      let iconBg = 'rgba(59, 130, 246, 0.15)';
      let title = 'Workspace activity';

      if (act.includes('LOGIN') && !act.includes('FAIL')) {
        icon = 'login';
        iconColor = '#8B5CF6';
        iconBg = 'rgba(139, 92, 246, 0.12)';
        title = 'User logged in';
      } else if (act.includes('FAIL')) {
        icon = 'warning';
        iconColor = '#EF4444';
        iconBg = 'rgba(239, 68, 68, 0.12)';
        title = 'Failed login attempt';
      } else if (act.includes('PROJECT') && act.includes('CREATE')) {
        icon = 'folder';
        iconColor = '#2563EB';
        iconBg = 'rgba(37, 99, 235, 0.15)';
        title = 'Project created';
      } else if (act.includes('TASK') && act.includes('COMPLETE')) {
        icon = 'check_circle';
        iconColor = '#10B981';
        iconBg = 'rgba(16, 185, 129, 0.12)';
        title = 'Task completed';
      } else if (act.includes('MEMBER') || act.includes('JOIN')) {
        icon = 'person_add';
        iconColor = '#3B82F6';
        iconBg = 'rgba(59, 130, 246, 0.12)';
        title = 'User joined project';
      } else {
        icon = 'corporate_fare';
        iconColor = '#3B82F6';
        iconBg = 'rgba(59, 130, 246, 0.15)';
        title = log.action ? log.action.replace(/_/g, ' ') : 'Workspace Activity';
      }

      return {
        id: log.id,
        icon,
        iconColor,
        iconBg,
        title,
        subtitle: log.description || 'System event recorded',
        timeAgo: this.formatRelativeTime(log.createdAt),
      };
    });
  }

  private populateTableRows(projects: Project[], tasks: TaskItem[]) {
    if (projects.length === 0) {
      this.tableRows = [];
      return;
    }

    this.tableRows = projects.map((p, idx) => {
      const projectTasks = tasks.filter(t => t.projectId === p.id);
      const completed = projectTasks.filter(t => t.status === 'Completed' || t.isCompleted).length;
      const total = projectTasks.length;
      const effectiveCompleted = completed;
      const pct = total > 0 ? Math.round((effectiveCompleted / total) * 100) : (p.progress || 0);

      const priority = p.priority || 'Medium';
      const priorityClass = priority.toLowerCase() === 'high' ? 'priority-high' : (priority.toLowerCase() === 'medium' ? 'priority-medium' : 'priority-low');

      let cleanSubtitle = 'Active Workspace';
      if (p.name?.includes('Booking')) cleanSubtitle = 'Client Scheduling Portal';
      else if (p.name?.includes('Employee')) cleanSubtitle = 'HR & Attendance Portal';
      else if (p.name?.includes('Inventory')) cleanSubtitle = 'Warehouse & Stock Tracking';
      else if (p.description) cleanSubtitle = p.description.length > 35 ? p.description.substring(0, 32) + '...' : p.description;

      return {
        id: p.id,
        name: p.name || 'Organization Project',
        subtitle: cleanSubtitle,
        initials: this.getInitials(p.name || 'PR'),
        priority,
        priorityClass,
        progressPercent: pct,
        completedTasksCount: effectiveCompleted,
        totalTasksCount: total,
        lastUpdated: this.formatRelativeTime(p.startDate),
        status: p.status || (pct === 100 ? 'Completed' : 'Active'),
        statusDotClass: (p.status === 'Completed' || pct === 100) ? 'status-completed' : 'status-active',
        isSelected: false,
      };
    });
  }

  toggleInteractiveDropdown(event: Event) {
    event.stopPropagation();
    this.interactiveDropdownOpen = !this.interactiveDropdownOpen;
    this.cdr.markForCheck();
  }

  selectInteractive(opt: string) {
    this.selectedInteractive = opt;
    this.interactiveDropdownOpen = false;
    this.cdr.markForCheck();
  }

  toggleSelectAll() {
    this.selectAll = !this.selectAll;
    this.tableRows.forEach(r => r.isSelected = this.selectAll);
    this.cdr.markForCheck();
  }

  toggleRowSelection(row: ProjectTableRow, event: Event) {
    event.stopPropagation();
    row.isSelected = !row.isSelected;
    this.selectAll = this.tableRows.every(r => r.isSelected);
    this.cdr.markForCheck();
  }

  onRowClick(row: ProjectTableRow) {
    this.router.navigate(['/tenant/projects']);
  }

  openAddProjectModal() {
    this.router.navigate(['/tenant/projects']);
  }

  viewAllActivities() {
    this.router.navigate(['/tenant/notifications']);
  }

  private getInitials(name?: string): string {
    if (!name) return 'PR';
    const words = name.trim().split(/\s+/);
    if (words.length >= 2) {
      return (words[0][0] + words[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }

  private formatRelativeTime(dateStr?: string | Date): string {
    if (!dateStr) return '1 day ago';
    const date = new Date(dateStr);
    const now = new Date();
    const diffMonths = (now.getFullYear() - date.getFullYear()) * 12 + (now.getMonth() - date.getMonth());
    if (diffMonths > 0) return `${diffMonths} months ago`;
    const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));
    if (diffDays > 0) return `${diffDays} days ago`;
    return 'Just now';
  }
}

