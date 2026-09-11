import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';

export interface AppNotification {
  id: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private readonly apiUrl = 'https://localhost:7134/api/Notification';

  public unreadCount = signal(0);
  public notifications = signal<AppNotification[]>([]);

  constructor(private http: HttpClient) {}

  loadNotifications(): void {
    this.http.get<any>(this.apiUrl).pipe(
      map(res => res.data || res),
      catchError((err) => {
        console.error('Error loading notifications:', err);
        return throwError(() => err);
      })
    ).subscribe({
      next: (list: AppNotification[]) => {
        this.notifications.set(list || []);
        this.unreadCount.set((list || []).filter(n => !n.isRead).length);
      },
      error: () => {
        this.notifications.set([]);
        this.unreadCount.set(0);
      }
    });
  }

  loadUnreadCount(): void {
    this.http.get<any>(`${this.apiUrl}/unread-count`).pipe(
      map(res => res.data ?? res),
      catchError((err) => {
        console.error('Error loading unread count:', err);
        return throwError(() => err);
      })
    ).subscribe({
      next: (count: number) => {
        this.unreadCount.set(count || 0);
      },
      error: () => {
        this.unreadCount.set(0);
      }
    });
  }

  markRead(id: string): Observable<boolean> {
    return this.http.put<any>(`${this.apiUrl}/${id}/mark-read`, {}).pipe(
      map(res => res.success ?? true),
      tap(() => {
        const current = this.notifications();
        const updated = current.map(n => n.id === id ? { ...n, isRead: true } : n);
        this.notifications.set(updated);
        this.unreadCount.set(updated.filter(n => !n.isRead).length);
      }),
      catchError((err) => {
        console.error(`Error marking notification ${id} as read:`, err);
        return throwError(() => err);
      })
    );
  }

  markAllRead(): Observable<boolean> {
    return this.http.put<any>(`${this.apiUrl}/mark-all-read`, {}).pipe(
      map(res => res.success ?? true),
      tap(() => {
        const updated = this.notifications().map(n => ({ ...n, isRead: true }));
        this.notifications.set(updated);
        this.unreadCount.set(0);
      }),
      catchError((err) => {
        console.error('Error marking all notifications as read:', err);
        return throwError(() => err);
      })
    );
  }

  deleteNotification(id: string): Observable<boolean> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`).pipe(
      map(res => res.success ?? true),
      tap(() => {
        const updated = this.notifications().filter(n => n.id !== id);
        this.notifications.set(updated);
        this.unreadCount.set(updated.filter(n => !n.isRead).length);
      }),
      catchError((err) => {
        console.error(`Error deleting notification ${id}:`, err);
        return throwError(() => err);
      })
    );
  }
}
