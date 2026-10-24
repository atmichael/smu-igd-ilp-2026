import jsPDF from 'jspdf';
import { APDocument } from '../types/ap';

/**
 * Generates an authentic Singapore Tax Invoice / Delivery Order / PO PDF
 * using jsPDF and triggers browser download.
 */
export const downloadDummyInvoicePdf = (doc: APDocument) => {
  const isCredit = doc.documentType === 'CREDIT_NOTE';
  const isDO = doc.documentType === 'DELIVERY_ORDER' || doc.documentType === 'GOODS_RECEIPT';
  const isSOA = doc.documentType === 'STATEMENT_OF_ACCOUNT';
  const isPO = doc.documentType === 'PURCHASE_ORDER';

  const docTitle = isCredit
    ? 'CREDIT NOTE'
    : isDO
    ? 'DELIVERY ORDER'
    : isSOA
    ? 'STATEMENT OF ACCOUNT'
    : isPO
    ? 'PURCHASE ORDER'
    : 'TAX INVOICE';

  const pdf = new jsPDF({
    orientation: 'portrait',
    unit: 'mm',
    format: 'a4',
  });

  // Color palette
  const darkNavy = [15, 23, 42]; // #0f172a
  const slateText = [51, 65, 85]; // #334155
  const slateMuted = [100, 116, 139]; // #64748b
  const emeraldGreen = [5, 150, 105]; // #059669
  const borderLight = [226, 232, 240]; // #e2e8f0

  // 1. Top Decorative Bar
  pdf.setFillColor(isCredit ? 217 : isDO ? 37 : isPO ? 124 : 5, isCredit ? 119 : isDO ? 99 : isPO ? 58 : 150, isCredit ? 6 : isDO ? 235 : isPO ? 237 : 105);
  pdf.rect(0, 0, 210, 5, 'F');

  // 2. Vendor Header (Left)
  pdf.setFont('helvetica', 'bold');
  pdf.setFontSize(16);
  pdf.setTextColor(darkNavy[0], darkNavy[1], darkNavy[2]);
  pdf.text(doc.vendor.name, 15, 18);

  pdf.setFont('helvetica', 'normal');
  pdf.setFontSize(9);
  pdf.setTextColor(slateMuted[0], slateMuted[1], slateMuted[2]);
  pdf.text(doc.vendor.address || 'Singapore', 15, 24);
  pdf.text(`Singapore UEN: ${doc.vendor.uen || 'N/A'}`, 15, 29);
  if (doc.vendor.gstRegNo) {
    pdf.text(`IRAS GST Reg No: ${doc.vendor.gstRegNo}`, 15, 34);
  }

  // 3. Document Badge & Number (Right)
  pdf.setFillColor(248, 250, 252);
  pdf.setDrawColor(borderLight[0], borderLight[1], borderLight[2]);
  pdf.roundedRect(135, 12, 60, 26, 2, 2, 'FD');

  pdf.setFont('helvetica', 'bold');
  pdf.setFontSize(11);
  pdf.setTextColor(isCredit ? 180 : isDO ? 30 : isPO ? 100 : 5, isCredit ? 83 : isDO ? 64 : isPO ? 40 : 120, isCredit ? 9 : isDO ? 175 : isPO ? 200 : 80);
  pdf.text(docTitle, 165, 19, { align: 'center' });

  pdf.setFontSize(10);
  pdf.setTextColor(darkNavy[0], darkNavy[1], darkNavy[2]);
  pdf.text(doc.documentNumber, 165, 26, { align: 'center' });

  pdf.setFont('helvetica', 'normal');
  pdf.setFontSize(8);
  pdf.setTextColor(slateMuted[0], slateMuted[1], slateMuted[2]);
  pdf.text(`Date: ${doc.issueDate}`, 165, 33, { align: 'center' });

  // 4. Billed To & References Section
  pdf.setDrawColor(borderLight[0], borderLight[1], borderLight[2]);
  pdf.line(15, 42, 195, 42);

  pdf.setFont('helvetica', 'bold');
  pdf.setFontSize(8);
  pdf.setTextColor(slateMuted[0], slateMuted[1], slateMuted[2]);
  pdf.text('BILLED TO (CUSTOMER):', 15, 48);
  pdf.text('PAYMENT & ORDER DETAILS:', 115, 48);

  pdf.setFont('helvetica', 'bold');
  pdf.setFontSize(10);
  pdf.setTextColor(darkNavy[0], darkNavy[1], darkNavy[2]);
  pdf.text('LionCity Trading & Engineering Pte Ltd', 15, 54);

  pdf.setFont('helvetica', 'normal');
  pdf.setFontSize(8.5);
  pdf.setTextColor(slateText[0], slateText[1], slateText[2]);
  pdf.text('UEN: 201809122K | GST Reg: M9-0091823-X', 15, 59);
  pdf.text('120 Robinson Road, #08-01, Singapore 068913', 15, 64);

  pdf.text(`Due Date: ${doc.dueDate}`, 115, 54);
  pdf.text(`Payment Terms: Net ${doc.paymentTermsDays || 30} Days`, 115, 59);
  if (doc.referenceNumbers && doc.referenceNumbers.length > 0) {
    pdf.text(`Ref / PO / DO: ${doc.referenceNumbers.join(', ')}`, 115, 64);
  } else {
    pdf.text(`Currency: ${doc.currency}`, 115, 64);
  }

  // 5. Line Items Table Header
  let yPos = 73;
  pdf.setFillColor(241, 245, 249);
  pdf.rect(15, yPos, 180, 8, 'F');

  pdf.setFont('helvetica', 'bold');
  pdf.setFontSize(8.5);
  pdf.setTextColor(slateText[0], slateText[1], slateText[2]);
  pdf.text('DESCRIPTION / SPECIFICATION', 18, yPos + 5.5);
  pdf.text('QTY', 120, yPos + 5.5, { align: 'center' });
  pdf.text(`UNIT (${doc.currency})`, 150, yPos + 5.5, { align: 'right' });
  pdf.text(`AMOUNT (${doc.currency})`, 192, yPos + 5.5, { align: 'right' });

  // 6. Line Items Rows
  yPos += 8;
  pdf.setFont('helvetica', 'normal');
  pdf.setFontSize(8.5);

  doc.lineItems.forEach((item, index) => {
    // Alternating background
    if (index % 2 === 1) {
      pdf.setFillColor(248, 250, 252);
      pdf.rect(15, yPos, 180, 7.5, 'F');
    }

    pdf.setTextColor(darkNavy[0], darkNavy[1], darkNavy[2]);
    const cleanDesc = item.description.length > 55 ? item.description.substring(0, 52) + '...' : item.description;
    pdf.text(cleanDesc, 18, yPos + 5);

    pdf.text(item.quantity.toString(), 120, yPos + 5, { align: 'center' });
    pdf.text(`$${item.unitPrice.toFixed(2)}`, 150, yPos + 5, { align: 'right' });
    pdf.text(`$${item.amount.toFixed(2)}`, 192, yPos + 5, { align: 'right' });

    pdf.setDrawColor(borderLight[0], borderLight[1], borderLight[2]);
    pdf.line(15, yPos + 7.5, 195, yPos + 7.5);

    yPos += 7.5;
  });

  // 7. Totals Box
  yPos += 5;
  const totalsX = 125;
  pdf.setFont('helvetica', 'normal');
  pdf.setFontSize(9);
  pdf.setTextColor(slateText[0], slateText[1], slateText[2]);

  pdf.text('Subtotal (Excl. GST):', totalsX, yPos + 4);
  pdf.text(`$${doc.subtotal.toFixed(2)}`, 192, yPos + 4, { align: 'right' });

  pdf.text(`Singapore GST (${doc.gstRate}%):`, totalsX, yPos + 9);
  pdf.text(`$${doc.gstAmount.toFixed(2)}`, 192, yPos + 9, { align: 'right' });

  // Grand Total Line
  pdf.setDrawColor(darkNavy[0], darkNavy[1], darkNavy[2]);
  pdf.setLineWidth(0.5);
  pdf.line(totalsX, yPos + 12, 195, yPos + 12);

  pdf.setFont('helvetica', 'bold');
  pdf.setFontSize(11);
  pdf.setTextColor(darkNavy[0], darkNavy[1], darkNavy[2]);
  pdf.text('Total Payable:', totalsX, yPos + 18);
  pdf.text(`SGD $${doc.totalAmount.toFixed(2)}`, 192, yPos + 18, { align: 'right' });

  pdf.line(totalsX, yPos + 20, 195, yPos + 20);

  // 8. Singapore Payment Details Box (PayNow UEN)
  const payBoxY = Math.max(yPos + 26, 175);
  pdf.setFillColor(240, 253, 244); // light emerald
  pdf.setDrawColor(187, 247, 208);
  pdf.setLineWidth(0.2);
  pdf.roundedRect(15, payBoxY, 180, 22, 2, 2, 'FD');

  pdf.setFont('helvetica', 'bold');
  pdf.setFontSize(9);
  pdf.setTextColor(emeraldGreen[0], emeraldGreen[1], emeraldGreen[2]);
  pdf.text('SINGAPORE FAST / PAYNOW PAYMENT INSTRUCTIONS', 20, payBoxY + 6);

  pdf.setFont('helvetica', 'normal');
  pdf.setFontSize(8.5);
  pdf.setTextColor(slateText[0], slateText[1], slateText[2]);
  pdf.text(`PayNow Corporate UEN: ${doc.vendor.payNowUen || doc.vendor.uen || 'N/A'}`, 20, payBoxY + 12);
  if (doc.vendor.bankName) {
    pdf.text(`Bank: ${doc.vendor.bankName} | Account: ${doc.vendor.bankAccountNumber || 'N/A'}`, 20, payBoxY + 17);
  }

  pdf.setFont('helvetica', 'bold');
  pdf.setFontSize(10);
  pdf.setTextColor(darkNavy[0], darkNavy[1], darkNavy[2]);
  pdf.text('SGQR PayNow Enabled', 188, payBoxY + 12, { align: 'right' });

  // 9. Compliance Footer
  pdf.setFont('helvetica', 'normal');
  pdf.setFontSize(7.5);
  pdf.setTextColor(slateMuted[0], slateMuted[1], slateMuted[2]);
  pdf.text(
    'This is a demonstration Accounts Payable document formatted in accordance with IRAS Singapore e-Tax Guidelines.',
    105,
    285,
    { align: 'center' }
  );

  // Save the PDF
  const cleanName = doc.fileName.replace(/\.pdf$/i, '');
  pdf.save(`${cleanName}_DemoInvoice.pdf`);
};

/**
 * Creates a real client-side File (Blob) containing valid PDF binary data
 * from an APDocument, ready for instant upload/testing.
 */
export const createDummyPdfFileFromDoc = (doc: APDocument): File => {
  const isCredit = doc.documentType === 'CREDIT_NOTE';
  const isDO = doc.documentType === 'DELIVERY_ORDER' || doc.documentType === 'GOODS_RECEIPT';
  const isSOA = doc.documentType === 'STATEMENT_OF_ACCOUNT';
  const isPO = doc.documentType === 'PURCHASE_ORDER';

  const docTitle = isCredit
    ? 'CREDIT NOTE'
    : isDO
    ? 'DELIVERY ORDER'
    : isSOA
    ? 'STATEMENT OF ACCOUNT'
    : isPO
    ? 'PURCHASE ORDER'
    : 'TAX INVOICE';

  const pdf = new jsPDF({
    orientation: 'portrait',
    unit: 'mm',
    format: 'a4',
  });

  pdf.setFont('helvetica', 'bold');
  pdf.setFontSize(16);
  pdf.text(doc.vendor.name, 15, 18);

  pdf.setFont('helvetica', 'normal');
  pdf.setFontSize(9);
  pdf.text(`Singapore UEN: ${doc.vendor.uen || 'N/A'}`, 15, 24);
  if (doc.vendor.gstRegNo) {
    pdf.text(`GST Reg No: ${doc.vendor.gstRegNo}`, 15, 29);
  }
  pdf.text(`Address: ${doc.vendor.address || 'Singapore'}`, 15, 34);

  pdf.setFont('helvetica', 'bold');
  pdf.setFontSize(12);
  pdf.text(`${docTitle}: ${doc.documentNumber}`, 140, 18);
  pdf.setFont('helvetica', 'normal');
  pdf.setFontSize(9);
  pdf.text(`Issue Date: ${doc.issueDate}`, 140, 24);
  pdf.text(`Due Date: ${doc.dueDate}`, 140, 29);
  if (doc.referenceNumbers.length > 0) {
    pdf.text(`Ref: ${doc.referenceNumbers.join(', ')}`, 140, 34);
  }

  let y = 48;
  pdf.text(`Billed To: LionCity Trading & Engineering Pte Ltd (UEN: 201809122K)`, 15, y);
  y += 10;

  pdf.setFont('helvetica', 'bold');
  pdf.text('Item Description', 15, y);
  pdf.text('Qty', 120, y);
  pdf.text('Unit Price', 145, y);
  pdf.text('Total (SGD)', 175, y);
  pdf.line(15, y + 2, 195, y + 2);
  y += 7;

  pdf.setFont('helvetica', 'normal');
  doc.lineItems.forEach((li) => {
    pdf.text(li.description.slice(0, 48), 15, y);
    pdf.text(li.quantity.toString(), 120, y);
    pdf.text(`$${li.unitPrice.toFixed(2)}`, 145, y);
    pdf.text(`$${li.amount.toFixed(2)}`, 175, y);
    y += 7;
  });

  y += 5;
  pdf.text(`Subtotal: SGD $${doc.subtotal.toFixed(2)}`, 130, y);
  y += 5;
  pdf.text(`Singapore 9% GST: SGD $${doc.gstAmount.toFixed(2)}`, 130, y);
  y += 5;
  pdf.setFont('helvetica', 'bold');
  pdf.text(`Total Amount: SGD $${doc.totalAmount.toFixed(2)}`, 130, y);
  y += 10;

  pdf.setFont('helvetica', 'normal');
  pdf.text(`PayNow Corporate UEN: ${doc.vendor.payNowUen || doc.vendor.uen || 'N/A'}`, 15, y);
  if (doc.vendor.bankName) {
    pdf.text(`Bank Account: ${doc.vendor.bankName} - ${doc.vendor.bankAccountNumber || 'N/A'}`, 15, y + 5);
  }

  const pdfOutput = pdf.output('blob');
  return new File([pdfOutput], doc.fileName.endsWith('.pdf') ? doc.fileName : `${doc.fileName}.pdf`, {
    type: 'application/pdf',
  });
};

/**
 * Creates a File object containing sample invoice text
 */
export const createDummyFileFromDoc = (doc: APDocument): File => {
  const content = `TAX INVOICE
Vendor: ${doc.vendor.name}
Singapore UEN: ${doc.vendor.uen}
GST Reg No: ${doc.vendor.gstRegNo || 'M9-0012938-X'}
Address: ${doc.vendor.address || 'Singapore'}

Invoice Number: ${doc.documentNumber}
Issue Date: ${doc.issueDate}
Due Date: ${doc.dueDate}
Currency: ${doc.currency}
Payment Terms: Net ${doc.paymentTermsDays || 30} Days
References: ${doc.referenceNumbers.join(', ')}

LINE ITEMS:
${doc.lineItems.map((li, i) => `${i + 1}. ${li.description} | Qty: ${li.quantity} | Unit Price: SGD ${li.unitPrice.toFixed(2)} | Line Amount: SGD ${li.amount.toFixed(2)} | GST: ${li.gstRate}%`).join('\n')}

SUBTOTAL: SGD ${doc.subtotal.toFixed(2)}
SINGAPORE 9% GST: SGD ${doc.gstAmount.toFixed(2)}
GRAND TOTAL PAYABLE: SGD ${doc.totalAmount.toFixed(2)}

PAYMENT DETAILS:
PayNow Corporate UEN: ${doc.vendor.payNowUen || doc.vendor.uen}
Bank: ${doc.vendor.bankName || 'DBS Bank Ltd'} (Account: ${doc.vendor.bankAccountNumber || '003-91823-1'})
`;

  const blob = new Blob([content], { type: 'text/plain' });
  return new File([blob], doc.fileName, { type: 'text/plain' });
};

/**
 * Generates an authentic Singapore Tax Invoice HTML representation
 */
export const downloadDummyInvoiceHtml = (doc: APDocument) => {
  downloadDummyInvoicePdf(doc);
};
