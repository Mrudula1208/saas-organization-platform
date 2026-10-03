// Small shared helpers used by forms and API error handling.

export function isValidEmail(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test((email || '').trim());
}

// Builds a user-friendly message from an HTTP/API error.
export function getErrorMessage(err: any, fallback: string): string {
  const body = err?.error;

  // API error body: { success: false, message: '...' }
  if (body && typeof body.message === 'string' && body.message) {
    return body.message;
  }

  // ASP.NET model validation body: { title: '...', errors: { Name: ['...'] } }
  if (body && body.errors && typeof body.errors === 'object') {
    const key = Object.keys(body.errors)[0];
    if (key && body.errors[key]?.length > 0) {
      return body.errors[key][0];
    }
    if (body.title) return body.title;
  }

  // Plain text error body (skip HTML error pages)
  if (typeof body === 'string' && body && !body.startsWith('<')) {
    return body;
  }

  if (err?.status === 401) return 'Your session has expired. Please log in again.';
  if (err?.status === 403) return 'You do not have permission to do this.';
  if (err?.status === 404) return 'The requested item was not found.';
  if (err?.status === 0) return 'Cannot reach the server. Please try again.';
  if (err?.status >= 500) return 'Something went wrong on the server. Please try again.';

  // Plain Error objects thrown in the app code
  if (err?.message && (err?.status === undefined || err?.status === null)) {
    return err.message;
  }

  return fallback;
}

/**
 * Triggers a real native browser HTTP download directly from the server endpoint.
 * The browser's native download manager receives the server's HTTP response headers:
 * Content-Disposition: attachment; filename="report.pdf" (or .xlsx)
 * This guarantees the exact filename and real extension (.pdf / .xlsx) without any
 * client-side Blob or Object URL UUID.
 */
export function triggerServerDownload(url: string, fileName?: string): void {
  if (typeof window === 'undefined') return;

  try {
    window.location.href = url;
  } catch {
    window.open(url, '_blank');
  }
}

/**
 * Triggers a real-life browser file download with the exact specified filename and extension.
 * Converts the file to a Data URL (Base64) to eliminate any Blob UUID (e.g. blob:http://.../uuid)
 * from the browser request. This guarantees Chrome names the file with the exact filename and .pdf extension.
 */
export function downloadBlobAsFile(blob: Blob, fileName: string, mimeType?: string): void {
  if (typeof window === 'undefined' || typeof document === 'undefined') return;

  const resolvedMime = mimeType || blob.type || 'application/octet-stream';

  // 1. Ensure fileName has the correct extension matching the mime type
  let safeFileName = (fileName || 'document').trim();
  if (resolvedMime === 'application/pdf' && !safeFileName.toLowerCase().endsWith('.pdf')) {
    safeFileName = safeFileName.replace(/\.[^/.]+$/, '') + '.pdf';
  } else if (
    (resolvedMime.includes('spreadsheet') || resolvedMime.includes('excel')) &&
    !safeFileName.toLowerCase().endsWith('.xlsx')
  ) {
    safeFileName = safeFileName.replace(/\.[^/.]+$/, '') + '.xlsx';
  } else if (resolvedMime.includes('json') && !safeFileName.toLowerCase().endsWith('.json')) {
    safeFileName = safeFileName.replace(/\.[^/.]+$/, '') + '.json';
  }

  // 2. Convert to Data URL (Base64) so there is NO blob UUID for Chrome to extract.
  const reader = new FileReader();
  reader.onloadend = () => {
    let dataUrl = reader.result as string;

    // Ensure the data URL has the exact mimeType specified
    if (resolvedMime && dataUrl.startsWith('data:')) {
      dataUrl = dataUrl.replace(/^data:[^;]+;base64,/, `data:${resolvedMime};base64,`);
    }

    const link = document.createElement('a');
    link.style.display = 'none';
    link.href = dataUrl;
    link.setAttribute('download', safeFileName);
    link.download = safeFileName;

    document.body.appendChild(link);
    link.click();

    setTimeout(() => {
      if (document.body.contains(link)) {
        document.body.removeChild(link);
      }
    }, 2000);
  };
  reader.readAsDataURL(blob);
}

/**
 * Opens a PDF or document Blob directly in a new browser tab for instant in-browser viewing.
 */
export function openBlobInNewTab(blob: Blob, mimeType = 'application/pdf'): void {
  if (typeof window === 'undefined') return;
  const typedBlob = new Blob([blob], { type: mimeType });
  const objectUrl = window.URL.createObjectURL(typedBlob);
  const win = window.open(objectUrl, '_blank');
  if (win) {
    win.focus();
  }
  // Delay revoke so the new tab finishes rendering
  setTimeout(() => {
    try {
      window.URL.revokeObjectURL(objectUrl);
    } catch {
      // Ignored
    }
  }, 120000);
}

/**
 * Safely parses any UTC date string from API/database and converts it to a local JavaScript Date object.
 * Fixes the issue where UTC dates without a trailing 'Z' are mistakenly parsed as local time.
 */
export function parseUtcDate(dateStr?: string | Date | null): Date | null {
  if (!dateStr) return null;
  if (dateStr instanceof Date) return isNaN(dateStr.getTime()) ? null : dateStr;
  let str = String(dateStr).trim();
  if (!str) return null;
  if (str.includes(' ') && !str.includes('T')) {
    str = str.replace(' ', 'T');
  }
  // If no timezone offset (+05:30 / Z) is present, append Z to force UTC interpretation
  if (!str.endsWith('Z') && !/[+-]\d{2}(:\d{2})?$/.test(str)) {
    str = `${str}Z`;
  }
  const d = new Date(str);
  return isNaN(d.getTime()) ? null : d;
}

/**
 * Formats a UTC date string into the user's real local browser date & time:
 * Example: "2026-09-30 09:35:18 AM"
 */
export function formatLocalDateTime(dateStr?: string | Date | null, includeSeconds = true): string {
  const d = parseUtcDate(dateStr);
  if (!d) return '—';

  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');

  let hours = d.getHours();
  const minutes = String(d.getMinutes()).padStart(2, '0');
  const seconds = String(d.getSeconds()).padStart(2, '0');
  const ampm = hours >= 12 ? 'PM' : 'AM';
  hours = hours % 12;
  hours = hours ? hours : 12;
  const strHours = String(hours).padStart(2, '0');

  if (includeSeconds) {
    return `${year}-${month}-${day} ${strHours}:${minutes}:${seconds} ${ampm}`;
  }
  return `${year}-${month}-${day} ${strHours}:${minutes} ${ampm}`;
}

/**
 * Formats a UTC date into clean local readable date: "30 Sep 2026"
 */
export function formatLocalDate(dateStr?: string | Date | null): string {
  const d = parseUtcDate(dateStr);
  if (!d) return '—';
  return d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
}

/**
 * Formats a UTC date into clean local date + time: "30 Sep 2026, 09:35 AM"
 */
export function formatLocalDateWithTime(dateStr?: string | Date | null): string {
  const d = parseUtcDate(dateStr);
  if (!d) return '—';
  const datePart = d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  let hours = d.getHours();
  const minutes = String(d.getMinutes()).padStart(2, '0');
  const ampm = hours >= 12 ? 'PM' : 'AM';
  hours = hours % 12;
  hours = hours ? hours : 12;
  const strHours = String(hours).padStart(2, '0');
  return `${datePart}, ${strHours}:${minutes} ${ampm}`;
}

/**
 * Formats a UTC date string into a real-time relative string ("Just now", "2m ago", "1h ago", "3d ago"):
 */
export function formatRelativeTime(dateStr?: string | Date | null): string {
  const d = parseUtcDate(dateStr);
  if (!d) return 'Just now';

  const diffMs = Date.now() - d.getTime();
  // Any event within 45 seconds (or slightly ahead due to server clock skew up to 2 mins)
  if (diffMs < 45000) return 'Just now';

  const diffSecs = Math.floor(diffMs / 1000);
  const diffMins = Math.floor(diffSecs / 60);
  if (diffMins < 60) return `${diffMins}m ago`;

  const diffHours = Math.floor(diffMins / 60);
  if (diffHours < 24) return `${diffHours}h ago`;

  const diffDays = Math.floor(diffHours / 24);
  if (diffDays < 30) return `${diffDays}d ago`;

  const diffMonths = Math.floor(diffDays / 30);
  return `${diffMonths} month${diffMonths > 1 ? 's' : ''} ago`;
}
