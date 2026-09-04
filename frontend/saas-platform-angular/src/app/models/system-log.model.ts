export interface SystemLog {
  id: string;
  action: string;
  description: string;
  userId: string | null;
  tenantId: string | null;
  createdAt: string;
}