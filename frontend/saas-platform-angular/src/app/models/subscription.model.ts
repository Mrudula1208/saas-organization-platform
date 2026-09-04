export interface SubscriptionPlan {
  id: string;
  name: string;
  price: number;
  maxUsers: number;
  maxProjects: number;
  storageLimitMB: number;
  isActive: boolean;
  createdAt: string;
}