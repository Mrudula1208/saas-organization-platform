export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
}

export interface PlatformSettings {
  platformName: string;
  supportEmail: string;
  maintenanceMode: boolean;
  allowRegistrations: boolean;
  mfaRequired: boolean;
  sessionTimeout: number;
  updatedAt?: string;
}
