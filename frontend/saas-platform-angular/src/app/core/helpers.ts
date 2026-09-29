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

