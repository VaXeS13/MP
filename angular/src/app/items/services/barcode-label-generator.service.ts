import { Injectable } from '@angular/core';
import { jsPDF } from 'jspdf';
import JsBarcode from 'jsbarcode';
import type { ItemSheetDto } from '@proxy/items/models';

@Injectable({
  providedIn: 'root'
})
export class BarcodeLabelGeneratorService {
  private readonly LABEL_WIDTH = 50; // mm
  private readonly LABEL_HEIGHT = 25; // mm
  private readonly MARGIN = 5; // mm
  private readonly LABELS_PER_ROW = 4;
  private readonly LABELS_PER_COLUMN = 10;

  constructor() {}

  generateLabels(sheet: ItemSheetDto): void {
    if (!sheet.items || sheet.items.length === 0) {
      console.warn('No items to generate labels for');
      return;
    }

    const pdf = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    let currentX = this.MARGIN;
    let currentY = this.MARGIN;
    let labelCount = 0;
    let pageCount = 0;

    sheet.items.forEach((sheetItem, index) => {
      if (!sheetItem.barcode) {
        return; // Skip items without barcodes
      }

      // Add new page if needed
      if (labelCount > 0 && labelCount % (this.LABELS_PER_ROW * this.LABELS_PER_COLUMN) === 0) {
        pdf.addPage();
        currentX = this.MARGIN;
        currentY = this.MARGIN;
        pageCount++;
      }

      // Calculate position
      const col = labelCount % this.LABELS_PER_ROW;
      const row = Math.floor((labelCount % (this.LABELS_PER_ROW * this.LABELS_PER_COLUMN)) / this.LABELS_PER_ROW);

      currentX = this.MARGIN + col * this.LABEL_WIDTH;
      currentY = this.MARGIN + row * this.LABEL_HEIGHT;

      // Generate barcode as canvas
      const canvas = document.createElement('canvas');
      try {
        JsBarcode(canvas, sheetItem.barcode, {
          format: 'CODE128',
          width: 2,
          height: 40,
          displayValue: true,
          fontSize: 12,
          margin: 2
        });

        // Add barcode image to PDF
        const barcodeImage = canvas.toDataURL('image/png');
        pdf.addImage(barcodeImage, 'PNG', currentX + 2, currentY + 2, this.LABEL_WIDTH - 4, 12);

        // Add item details
        pdf.setFontSize(8);

        // Booth number
        if (sheet.boothNumber) {
          pdf.text(`Booth: ${sheet.boothNumber}`, currentX + 2, currentY + 16);
        }

        // Item name
        if (sheetItem.item?.name) {
          const name = sheetItem.item.name.length > 20
            ? sheetItem.item.name.substring(0, 20) + '...'
            : sheetItem.item.name;
          pdf.text(name, currentX + 2, currentY + 19);
        }

        // Price
        if (sheetItem.item?.price !== undefined) {
          const price = `${sheetItem.item.price.toFixed(2)} ${sheetItem.item.currency || ''}`;
          pdf.text(price, currentX + 2, currentY + 22);
        }

        // Draw border
        pdf.setDrawColor(200, 200, 200);
        pdf.rect(currentX, currentY, this.LABEL_WIDTH, this.LABEL_HEIGHT);

      } catch (error) {
        console.error('Error generating barcode for item:', sheetItem.barcode, error);
      }

      labelCount++;
    });

    // Save PDF
    const fileName = `sheet_${sheet.id}_labels_${new Date().toISOString().split('T')[0]}.pdf`;
    pdf.save(fileName);
  }
}
