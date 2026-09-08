export interface User {
  id: string;
  fullName: string;
  email: string;
  role: string;
  tenantId: string;
  isActive: boolean;
  profileImageUrl: string | null;
  createdAt: string;
  lastLogin: string;
  tenantName?: string;
  // Joined from the API so the super admin tables can show the real organization name.
  tenant?: { id?: string; name?: string };
}