import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LabelService {
  private apiUrl = `${environment.apis.default.url}/api/app/labels`;

  constructor(private http: HttpClient) {}

  generateItemLabel(itemSheetItemId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/item-sheet-item/${itemSheetItemId}`, {
      responseType: 'blob'
    });
  }

  generateMultipleLabels(itemSheetItemIds: string[]): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/multiple`, itemSheetItemIds, {
      responseType: 'blob'
    });
  }

  downloadLabel(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  printLabel(blob: Blob): void {
    const url = window.URL.createObjectURL(blob);
    const iframe = document.createElement('iframe');
    iframe.style.display = 'none';
    iframe.src = url;
    document.body.appendChild(iframe);

    iframe.onload = () => {
      iframe.contentWindow?.print();
      setTimeout(() => {
        document.body.removeChild(iframe);
        window.URL.revokeObjectURL(url);
      }, 1000);
    };
  }
}