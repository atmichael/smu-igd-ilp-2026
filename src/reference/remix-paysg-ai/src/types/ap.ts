export type DocumentType =
  | 'TAX_INVOICE'
  | 'PROFORMA_INVOICE'
  | 'CREDIT_NOTE'
  | 'DEBIT_NOTE'
  | 'STATEMENT_OF_ACCOUNT'
  | 'DELIVERY_ORDER'
  | 'GOODS_RECEIPT'
  | 'SERVICE_ORDER'
  | 'PURCHASE_ORDER';

export type PostingVerdict = 'APPROVED' | 'REQUIRES_ATTENTION' | 'CRITICAL_BLOCKER';

export type ThreeWayMatchStatus =
  | 'FULL_MATCH'
  | 'PARTIAL_MATCH'
  | 'PRICE_DISCREPANCY'
  | 'QUANTITY_DISCREPANCY'
  | 'NO_MATCHING_DOCS'
  | 'NOT_APPLICABLE';

export interface ValidationCheck {
  id: string;
  category: 'MATH' | 'GST_IRAS' | 'DUPLICATE' | 'MATCH' | 'VENDOR';
  title: string;
  status: 'PASSED' | 'WARNING' | 'FAILED';
  description: string;
}

export interface LineItem {
  id: string;
  itemCode?: string;
  description: string;
  quantity: number;
  unitPrice: number;
  amount: number;
  gstRate: number; // e.g. 9
  gstAmount: number;
  xeroAccountCode: string;
  xeroAccountName: string;
  poItemMatched?: boolean;
  doQuantityDelivered?: number;
}

export interface VendorInfo {
  name: string;
  uen?: string; // Singapore Unique Entity Number e.g. 201812345K
  gstRegNo?: string; // e.g. M90345678X
  address?: string;
  contactEmail?: string;
  contactPhone?: string;
  bankName?: string;
  bankAccountNumber?: string;
  payNowUen?: string;
}

export interface ThreeWayMatchResult {
  status: ThreeWayMatchStatus;
  matchedPoNumber?: string;
  matchedDoNumber?: string;
  quantityMatch: boolean;
  priceMatch: boolean;
  poAmount?: number;
  invoiceAmount?: number;
  varianceAmount: number;
  details: string[];
}

export interface XeroSyncPayload {
  contactName: string;
  type: 'ACCPAY';
  invoiceNumber: string;
  reference?: string;
  date: string;
  dueDate: string;
  lineAmountTypes: 'Exclusive' | 'Inclusive';
  currency: string;
  lineItems: Array<{
    description: string;
    quantity: number;
    unitAmount: number;
    accountCode: string;
    taxType: string;
    lineAmount: number;
  }>;
  status: 'DRAFT' | 'SUBMITTED' | 'AUTHORISED';
}

export interface APDocument {
  id: string;
  source: 'GMAIL' | 'MANUAL_UPLOAD' | 'SAMPLE';
  sourceEmail?: {
    messageId: string;
    subject: string;
    sender: string;
    receivedAt: string;
    snippet?: string;
    hasAttachment: boolean;
  };
  fileName: string;
  fileType: string;
  fileSizeFormatted?: string;
  rawText?: string;
  documentType: DocumentType;
  confidenceScore: number;
  vendor: VendorInfo;
  documentNumber: string;
  referenceNumbers: string[];
  issueDate: string;
  dueDate: string;
  paymentTermsDays?: number;
  currency: 'SGD' | 'USD' | string;
  subtotal: number;
  gstRate: number; // usually 9% in Singapore
  gstAmount: number;
  totalAmount: number;
  lineItems: LineItem[];
  postingVerdict: PostingVerdict;
  verdictReason: string;
  validationChecks: ValidationCheck[];
  threeWayMatch?: ThreeWayMatchResult;
  xeroPayload?: XeroSyncPayload;
  xeroPostStatus: {
    posted: boolean;
    xeroBillId?: string;
    postedAt?: string;
    statusBadge?: string;
  };
  notes?: string;
  createdAt: string;
  status: 'INGESTED' | 'AI_EXTRACTED' | 'NEEDS_REVIEW' | 'APPROVED' | 'READY_TO_POST' | 'POSTED_TO_XERO';
}

export interface CashflowForecastItem {
  period: 'Due Today' | '1 - 7 Days' | '8 - 14 Days' | '15 - 30 Days' | '31+ Days' | 'Overdue';
  count: number;
  totalSgd: number;
  documents: APDocument[];
}
