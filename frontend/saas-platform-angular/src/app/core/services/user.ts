import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { Auth } from './auth';

export interface User {
  id: string;
  fullName: string;
  email: string;
  role: string;
  tenantId: string;
  isActive: boolean;
  createdAt: string;
  lastLogin: string;
  profileImageUrl?: string;
  tenantName?: string;
}

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly apiUrl = 'https://localhost:7134/api/User';

  private mockUsers: User[] = [
    {
      id: 'user-1',
      fullName: 'Jann Sanner',
      email: 'sanner@example.com',
      role: 'Admin',
      tenantId: '11112222-3333-4444-5555-666677778888',
      isActive: true,
      createdAt: '2024-06-10T12:00:00Z',
      lastLogin: 'Today, 09:20 AM',
      profileImageUrl: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=100',
      tenantName: 'Acme Corp'
    },
    {
      id: 'user-2',
      fullName: 'Emma Smith',
      email: 'cemet@email.com',
      role: 'Manager',
      tenantId: '11112222-3333-4444-5555-666677778888',
      isActive: true,
      createdAt: '2024-06-12T09:30:00Z',
      lastLogin: 'Yesterday, 14:45 PM',
      profileImageUrl: 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&q=80&w=100',
      tenantName: 'Acme Corp'
    },
    {
      id: 'user-3',
      fullName: 'Michael Brown',
      email: 'saran@email.com',
      role: 'Member',
      tenantId: '11112222-3333-4444-5555-666677778888',
      isActive: true,
      createdAt: '2024-06-10T15:00:00Z',
      lastLogin: '10 Jun 2024',
      profileImageUrl: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&q=80&w=100',
      tenantName: 'Acme Corp'
    },
    {
      id: 'user-4',
      fullName: 'Sarah Johnson',
      email: 'adme@email.com',
      role: 'Admin',
      tenantId: '11112222-3333-4444-5555-666677778888',
      isActive: true,
      createdAt: '2024-06-10T10:15:00Z',
      lastLogin: '10 Jun 2024, Seen: 21:30 AM',
      profileImageUrl: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?auto=format&fit=crop&q=80&w=100',
      tenantName: 'Acme Corp'
    },
    {
      id: 'user-5',
      fullName: 'Kevin White',
      email: 'john@email.com',
      role: 'Member',
      tenantId: '11112222-3333-4444-5555-666677778888',
      isActive: false,
      createdAt: '2024-06-10T11:20:00Z',
      lastLogin: '10 Jun 2024',
      profileImageUrl: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&q=80&w=100',
      tenantName: 'Acme Corp'
    },
    {
      id: 'user-6',
      fullName: 'Laura Wilson',
      email: 'laura@email.com',
      role: 'Manager',
      tenantId: '11112222-3333-4444-5555-666677778888',
      isActive: true, // Suspended state in mock can toggle
      createdAt: '2024-06-10T13:40:00Z',
      lastLogin: '10 Jun 2024',
      profileImageUrl: 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&q=80&w=100',
      tenantName: 'Acme Corp'
    },
    {
      id: 'user-7',
      fullName: 'David Clark',
      email: 'dauid@email.com',
      role: 'Member',
      tenantId: '99998888-7777-6666-5555-444433332222',
      isActive: true,
      createdAt: '2024-06-10T14:10:00Z',
      lastLogin: '10 Jun 2024',
      profileImageUrl: 'https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?auto=format&fit=crop&q=80&w=100',
      tenantName: 'Globex Corporation'
    },
    {
      id: 'user-8',
      fullName: 'Amy Lee',
      email: 'rclmat@email.com',
      role: 'TenantAdmin',
      tenantId: '99998888-7777-6666-5555-444433332222',
      isActive: true,
      createdAt: '2024-06-10T16:50:00Z',
      lastLogin: '10 Jun 2024',
      profileImageUrl: 'https://images.unsplash.com/photo-1554151228-14d9def656e4?auto=format&fit=crop&q=80&w=100',
      tenantName: 'Globex Corporation'
    }
  ];

  constructor(private http: HttpClient, private auth: Auth) {}

  private getHeaders(): HttpHeaders {
    // Read local JWT token and attach Authorization header
    if (typeof window !== 'undefined') {
      const token = localStorage.getItem('saas_token');
      if (token) {
        return new HttpHeaders().set('Authorization', `Bearer ${token}`);
      }
    }
    return new HttpHeaders();
  }

  getUsers(): Observable<User[]> {
    const tenantId = this.auth.getTenantId();
    return this.http.get<User[]>(this.apiUrl, { headers: this.getHeaders() }).pipe(
      catchError(() => {
        console.warn('User API offline. Filtering local mock users.');
        if (!tenantId) {
          // If Super Admin, return all
          return of([...this.mockUsers]);
        }
        return of(this.mockUsers.filter(u => u.tenantId === tenantId));
      })
    );
  }

  getUserById(id: string): Observable<User | null> {
    return this.http.get<any>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() }).pipe(
      map(res => res.data || res),
      catchError(() => {
        console.warn(`User API offline. Finding mock user: ${id}`);
        const found = this.mockUsers.find(u => u.id === id) || null;
        return of(found);
      })
    );
  }

  createUser(dto: any): Observable<User> {
    const tenantId = this.auth.getTenantId() || dto.tenantId || '11112222-3333-4444-5555-666677778888';
    
    // Structure expected by API
    const userPayload = {
      name: dto.fullName || dto.name,
      email: dto.email,
      password: dto.password || 'password123',
      tenantId: tenantId
    };

    const newUser: User = {
      id: crypto.randomUUID(),
      fullName: userPayload.name,
      email: userPayload.email,
      role: dto.role || 'Member',
      tenantId: tenantId,
      isActive: true,
      createdAt: new Date().toISOString(),
      lastLogin: 'Never',
      profileImageUrl: dto.profileImageUrl || 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&q=80&w=100',
      tenantName: 'Acme Corp'
    };

    return this.http.post<any>(this.apiUrl, userPayload, { headers: this.getHeaders() }).pipe(
      map(res => res.data || res),
      tap((resUser) => {
        this.mockUsers.push(resUser);
      }),
      catchError(() => {
        console.warn('User API creation failed. Storing in mock cache.');
        this.mockUsers.push(newUser);
        return of(newUser);
      })
    );
  }

  updateUser(id: string, user: any): Observable<boolean> {
    return this.http.put(`${this.apiUrl}/${id}`, user, { headers: this.getHeaders() }).pipe(
      map(() => true),
      catchError(() => {
        console.warn(`User API update failed. Updating mock user ID: ${id}`);
        const idx = this.mockUsers.findIndex(u => u.id === id);
        if (idx !== -1) {
          this.mockUsers[idx] = { ...this.mockUsers[idx], ...user };
          return of(true);
        }
        return of(false);
      })
    );
  }

  deleteUser(id: string): Observable<boolean> {
    return this.http.delete(`${this.apiUrl}/${id}`, { headers: this.getHeaders() }).pipe(
      map(() => true),
      catchError(() => {
        console.warn(`User API deletion failed. Removing mock user ID: ${id}`);
        const initialLength = this.mockUsers.length;
        this.mockUsers = this.mockUsers.filter(u => u.id !== id);
        return of(this.mockUsers.length < initialLength);
      })
    );
  }
}
