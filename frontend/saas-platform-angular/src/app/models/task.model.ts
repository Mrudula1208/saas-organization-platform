export interface TaskItem {
  id: string;
  name: string;
  description: string;
  status: string;
  priority: string;
  dueDate: string;
  isCompleted: boolean;
  completedAt: string | null;
  projectId: string;
  assignedUserId: string;
  createdAt: string;
  projectName?: string;
  assignedUserName?: string;
}