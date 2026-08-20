import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
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

  private mockNotifications: AppNotification[] = [
    { id: 'mock-1', message: 'Task assigned for Website Redesign has been assigned to Jann Sanner.', isRead: false, createdAt: '2026-05-24T14:30:00Z' },
    { id: 'mock-2', message: 'Milestone achieved for Database Migration: schema migration successfully verified.', isRead: false, createdAt: '2026-05-23T11:15:00Z' },
    { id: 'mock-3', message: 'Task completed: Michael Brown finished "Automaticianane decision tasks".', isRead: true, createdAt: '2026-05-22T09:00:00Z' },
  ];

  constructor(private http: HttpClient) {}

  loadNotifications(): void {
    this.http.get<any>(this.apiUrl).pipe(
      map(res => res.data || res),
      catchError(() => {
        console.warn('Notification API offline. Using mock notifications.');
        return of(this.mockNotifications);
      })
    ).subscribe((list: AppNotification[]) => {
      this.notifications.set(list);
      this.unreadCount.set(list.filter(n => !n.isRead).length);
    });
  }

  loadUnreadCount(): void {
    this.http.get<any>(`${this.apiUrl}/unread-count`).pipe(
      map(res => res.data ?? res),
      catchError(() => {
        return of(this.mockNotifications.filter(n => !n.isRead).length);
      })
    ).subscribe((count: number) => {
      this.unreadCount.set(count);
    });
  }

  markRead(id: string): Observable<boolean> {
    return this.http.put<any>(`${this.apiUrl}/${id}/mark-read`, {}).pipe(
      map(() => true),
      tap(() => {
        const current = this.notifications();
        const updated = current.map(n => n.id === id ? { ...n, isRead: true } : n);
        this.notifications.set(updated);
        this.unreadCount.set(updated.filter(n => !n.isRead).length);
      }),
      catchError(() => {
        const current = this.notifications();
        const updated = current.map(n => n.id === id ? { ...n, isRead: true } : n);
        this.notifications.set(updated);
        this.unreadCount.set(updated.filter(n => !n.isRead).length);
        return of(true);
      })
    );
  }

  markAllRead(): Observable<boolean> {
    return this.http.put<any>(`${this.apiUrl}/mark-all-read`, {}).pipe(
      map(() => true),
      tap(() => {
        const updated = this.notifications().map(n => ({ ...n, isRead: true }));
        this.notifications.set(updated);
        this.unreadCount.set(0);
      }),
      catchError(() => {
        const updated = this.notifications().map(n => ({ ...n, isRead: true }));
        this.notifications.set(updated);
        this.unreadCount.set(0);
        return of(true);
      })
    );
  }

  deleteNotification(id: string): Observable<boolean> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`).pipe(
      map(() => true),
      tap(() => {
        const updated = this.notifications().filter(n => n.id !== id);
        this.notifications.set(updated);
        this.unreadCount.set(updated.filter(n => !n.isRead).length);
      }),
      catchError(() => {
        const updated = this.notifications().filter(n => n.id !== id);
        this.notifications.set(updated);
        this.unreadCount.set(updated.filter(n => !n.isRead).length);
        return of(true);
      })
    );
  }
}
