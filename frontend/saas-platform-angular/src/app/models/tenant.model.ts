export interface Tenant {
  id: string;
  name: string;
  domain: string;
  contactEmail: string;
  contactPhone: string;
  subscriptionPlanId: string;
  isActive: boolean;
  logoImageUrl: string | null;
  isDeleted: boolean;
  createdAt: string;
  plan?: string;
  status?: string;
  usersCount?: number;
  projectsCount?: number;
  monthlyRevenue?: number;
}

export interface TenantSettings {
  id: string;
  name: string;
  domain: string;
  contactEmail: string;
  contactPhone: string;
  logoImageUrl: string;
  emailNotifications: boolean;
  inAppNotifications: boolean;
}