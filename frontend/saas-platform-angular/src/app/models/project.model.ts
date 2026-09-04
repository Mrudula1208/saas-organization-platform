export interface Project {
  id: string;
  name: string;
  description: string;
  startDate: string;
  endDate: string;
  priority: string;
  status: string;
  ownerId: string;
  ownerName: string;
  tenantId: string;
  progress: number;
  isActive: boolean;
  createdAt: string;
  taskCount: number;
  completedTaskCount: number;
}

export interface UpdateProjectPayload {
  name: string;
  description: string;
  status: string;
  priority: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export interface ProjectMember {
  id: string;
  projectId: string;
  projectName: string;
  userId: string;
  userFullName: string;
  userEmail: string;
  userRole: string;
  userProfileImageUrl: string | null;
}