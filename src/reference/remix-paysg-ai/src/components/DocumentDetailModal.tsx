import React, { useState } from 'react';
import {
  X,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  QrCode,
  Building,
  Calendar,
  Layers,
  ArrowRight,
  Send,
  Download,
  Copy,
  ExternalLink,
  ShieldCheck,
  Sparkles,
  FileText,
  Mail,
  RefreshCw,
  Printer,
  Upload,
} from 'lucide-react';
import { APDocument, LineItem } from '../types/ap';
import { downloadDummyInvoicePdf } from '../utils/pdfGenerator';

interface DocumentDetailModalProps {
  document: APDocument | null;
  onClose: () => void;
  onPostToXero: (doc: APDocument) => void;
  onUpdateLineItem: (docId: string, updatedLineItems: LineItem[]) => void;
  onApproveDocument: (docId: string) => void;
  onOpenThreeWayMatch: (doc: APDocument) => void;
  onOpenUpload?: () => void;
}

export const DocumentDetailModal: React.FC<DocumentDetailModalProps> = ({
  document,
  onClose,
  onPostToXero,
  onUpdateLineItem,
  onApproveDocument,
  onOpenThreeWayMatch,
  onOpenUpload,
}) => {
  if (!document) return null;

  const [activeTab, setActiveTab] = useState<'overview' | 'preview' | 'lineItems' | 'checks' | 'paynow' | 'xero'>('overview');
  const [copiedPayNow, setCopiedPayNow] = useState(false);
  const [showCreditNoteDraft, setShowCreditNoteDraft] = useState(false);

  const handleCopyPayNow = () => {
    if (document.vendor.payNowUen) {
      navigator.clipboard.writeText(document.vendor.payNowUen);
      setCopiedPayNow(true);
      setTimeout(() => setCopiedPayNow(false), 2000);
    }
  };

  const handleDownloadPdf = () => {
    downloadDummyInvoicePdf(document);
  };

  const getVerdictStyle = () => {
    switch (document.postingVerdict) {
      case 'APPROVED':
        return {
          bg: 'bg-emerald-50 border-emerald-200 text-emerald-900',
          badge: 'bg-emerald-600 text-white',
          icon: <CheckCircle2 className="w-5 h-5 text-emerald-600" />,
          title: 'Approved for Accounting Posting',
        };
      case 'REQUIRES_ATTENTION':
        return {
          bg: 'bg-amber-50 border-amber-200 text-amber-900',
          badge: 'bg-amber-600 text-white',
          icon: <AlertTriangle className="w-5 h-5 text-amber-600" />,
          title: 'Review Required Before Posting',
        };
      case 'CRITICAL_BLOCKER':
        return {
          bg: 'bg-rose-50 border-rose-200 text-rose-900',
          badge: 'bg-rose-600 text-white',
          icon: <XCircle className="w-5 h-5 text-rose-600" />,
          title: 'Posting Blocked by AP Rules',
        };
    }
  };

  const verdict = getVerdictStyle();

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-3 sm:p-4">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        {/* Modal Top Header */}
        <div className="p-4 sm:p-5 bg-slate-900 text-white flex items-center justify-between border-b border-slate-800">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 bg-emerald-500/20 border border-emerald-500/40 rounded-xl text-emerald-400">
              <FileText className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h3 className="text-base font-bold text-white">{document.documentNumber}</h3>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700">
                  {document.documentType.replace(/_/g, ' ')}
                </span>
                {document.source === 'GMAIL' && (
                  <span className="text-[10px] font-medium px-2 py-0.5 rounded bg-blue-500/20 text-blue-300 border border-blue-500/30">
                    Ingested from Gmail
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-400">
                Supplier: <span className="text-slate-200 font-semibold">{document.vendor.name}</span>
                {document.vendor.uen && ` &bull; UEN: ${document.vendor.uen}`}
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <button
              onClick={onClose}
              className="p-1.5 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* AI Posting Verdict Header Banner */}
        <div className={`p-4 border-b flex items-start space-x-3 ${verdict.bg}`}>
          <div className="mt-0.5">{verdict.icon}</div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center space-x-2">
              <span className="font-bold text-xs uppercase tracking-wider">{verdict.title}</span>
              <span className="text-[10px] px-1.5 py-0.2 rounded font-semibold bg-white/70 text-slate-800">
                Confidence: {Math.round(document.confidenceScore * 100)}%
              </span>
            </div>
            <p className="text-xs mt-1 text-slate-800 font-medium">{document.verdictReason}</p>
          </div>

          {/* Quick Header CTA */}
          <div className="shrink-0 flex items-center space-x-2">
            <button
              onClick={handleDownloadPdf}
              className="flex items-center space-x-1.5 px-3 py-1.5 bg-white border border-slate-300 hover:bg-slate-100 text-slate-700 rounded-lg text-xs font-semibold shadow-xs transition"
              title="Download authentic Singapore dummy Tax Invoice PDF"
            >
              <Download className="w-3.5 h-3.5 text-emerald-600" />
              <span>Download Dummy PDF</span>
            </button>

            {!document.xeroPostStatus.posted && document.documentType === 'TAX_INVOICE' && (
              <button
                onClick={() => onPostToXero(document)}
                className="flex items-center space-x-1.5 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-bold shadow-sm transition"
              >
                <Send className="w-3.5 h-3.5" />
                <span>Post to Xero</span>
              </button>
            )}
          </div>
        </div>

        {/* Modal Tab Bar */}
        <div className="flex border-b border-slate-200 bg-slate-50 px-4 text-xs font-medium overflow-x-auto">
          <button
            onClick={() => setActiveTab('overview')}
            className={`py-2.5 px-3 border-b-2 font-semibold transition whitespace-nowrap ${
              activeTab === 'overview'
                ? 'border-emerald-600 text-emerald-700 bg-white'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            Overview &amp; Amounts
          </button>
          <button
            onClick={() => setActiveTab('preview')}
            className={`py-2.5 px-3 border-b-2 font-semibold transition whitespace-nowrap flex items-center space-x-1.5 ${
              activeTab === 'preview'
                ? 'border-emerald-600 text-emerald-700 bg-white'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            <Printer className="w-3.5 h-3.5" />
            <span>Visual Document PDF</span>
          </button>
          <button
            onClick={() => setActiveTab('lineItems')}
            className={`py-2.5 px-3 border-b-2 font-semibold transition whitespace-nowrap ${
              activeTab === 'lineItems'
                ? 'border-emerald-600 text-emerald-700 bg-white'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            Line Items ({document.lineItems.length})
          </button>
          <button
            onClick={() => setActiveTab('checks')}
            className={`py-2.5 px-3 border-b-2 font-semibold transition ${
              activeTab === 'checks'
                ? 'border-emerald-600 text-emerald-700 bg-white'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            IRAS &amp; Math Checks ({document.validationChecks.length})
          </button>
          <button
            onClick={() => setActiveTab('paynow')}
            className={`py-2.5 px-3 border-b-2 font-semibold transition ${
              activeTab === 'paynow'
                ? 'border-emerald-600 text-emerald-700 bg-white'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            PayNow Singapore QR
          </button>
          <button
            onClick={() => setActiveTab('xero')}
            className={`py-2.5 px-3 border-b-2 font-semibold transition ${
              activeTab === 'xero'
                ? 'border-emerald-600 text-emerald-700 bg-white'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            Xero Bill Payload
          </button>
        </div>

        {/* Modal Tab Content Area */}
        <div className="flex-1 overflow-y-auto p-5 space-y-6">
          {/* TAB 1: OVERVIEW */}
          {activeTab === 'overview' && (
            <div className="space-y-6">
              {/* Financial Summary Cards */}
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                <div className="p-3 bg-slate-50 border border-slate-200 rounded-xl">
                  <span className="text-[11px] text-slate-500 font-medium">Subtotal (Excl. Tax)</span>
                  <div className="text-base font-bold text-slate-900 mt-1">
                    {document.currency} ${document.subtotal.toFixed(2)}
                  </div>
                </div>

                <div className="p-3 bg-emerald-50/50 border border-emerald-200 rounded-xl">
                  <span className="text-[11px] text-emerald-700 font-medium">
                    IRAS 9% GST Amount
                  </span>
                  <div className="text-base font-bold text-emerald-900 mt-1">
                    {document.currency} ${document.gstAmount.toFixed(2)}
                  </div>
                  <span className="text-[10px] text-emerald-600">Claimable Input Tax</span>
                </div>

                <div className="p-3 bg-slate-900 text-white rounded-xl">
                  <span className="text-[11px] text-slate-400 font-medium">Grand Total Payable</span>
                  <div className="text-base font-bold text-white mt-1">
                    {document.currency} ${document.totalAmount.toFixed(2)}
                  </div>
                </div>

                <div className="p-3 bg-slate-50 border border-slate-200 rounded-xl">
                  <span className="text-[11px] text-slate-500 font-medium">Payment Due Date</span>
                  <div className="text-sm font-bold text-slate-800 mt-1">{document.dueDate}</div>
                  <span className="text-[10px] text-slate-400">
                    Net {document.paymentTermsDays || 30} Days
                  </span>
                </div>
              </div>

              {/* Vendor & Banking Info Card */}
              <div className="p-4 bg-white border border-slate-200 rounded-xl shadow-xs space-y-3">
                <div className="flex items-center justify-between border-b border-slate-100 pb-2">
                  <h4 className="text-xs font-bold uppercase tracking-wider text-slate-600 flex items-center space-x-1.5">
                    <Building className="w-3.5 h-3.5 text-emerald-600" />
                    <span>Singapore Vendor Identity &amp; Banking</span>
                  </h4>
                  {document.vendor.gstRegNo && (
                    <span className="text-[11px] font-mono bg-emerald-100 text-emerald-800 px-2 py-0.5 rounded font-semibold">
                      GST Reg: {document.vendor.gstRegNo}
                    </span>
                  )}
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
                  <div className="space-y-1.5">
                    <div>
                      <span className="text-slate-400">Registered Entity:</span>{' '}
                      <span className="font-semibold text-slate-900">{document.vendor.name}</span>
                    </div>
                    {document.vendor.uen && (
                      <div>
                        <span className="text-slate-400">Singapore UEN:</span>{' '}
                        <span className="font-mono font-bold text-slate-800">{document.vendor.uen}</span>
                      </div>
                    )}
                    {document.vendor.address && (
                      <div>
                        <span className="text-slate-400">Address:</span>{' '}
                        <span className="text-slate-700">{document.vendor.address}</span>
                      </div>
                    )}
                  </div>

                  <div className="space-y-1.5 bg-slate-50 p-3 rounded-lg border border-slate-100">
                    <div className="font-semibold text-slate-800 flex items-center justify-between">
                      <span>Payment Method Details</span>
                      {document.vendor.payNowUen && (
                        <span className="text-[10px] text-emerald-700 font-bold bg-emerald-100 px-1.5 py-0.5 rounded">
                          PayNow Corporate
                        </span>
                      )}
                    </div>
                    {document.vendor.bankName && (
                      <div>
                        <span className="text-slate-500">Bank:</span>{' '}
                        <span className="text-slate-800 font-medium">{document.vendor.bankName}</span>
                      </div>
                    )}
                    {document.vendor.bankAccountNumber && (
                      <div>
                        <span className="text-slate-500">Account #:</span>{' '}
                        <span className="font-mono text-slate-800 font-medium">
                          {document.vendor.bankAccountNumber}
                        </span>
                      </div>
                    )}
                    {document.vendor.payNowUen && (
                      <div>
                        <span className="text-slate-500">PayNow UEN:</span>{' '}
                        <span className="font-mono font-bold text-emerald-800">
                          {document.vendor.payNowUen}
                        </span>
                      </div>
                    )}
                  </div>
                </div>
              </div>

              {/* 3-Way Match Summary if applicable */}
              {document.threeWayMatch && document.threeWayMatch.status !== 'NOT_APPLICABLE' && (
                <div className="p-4 bg-slate-50 border border-slate-200 rounded-xl space-y-2">
                  <div className="flex items-center justify-between">
                    <h4 className="text-xs font-bold uppercase tracking-wider text-slate-700 flex items-center space-x-1.5">
                      <Layers className="w-3.5 h-3.5 text-blue-600" />
                      <span>3-Way Match Reconciliation (PO vs DO vs Inv)</span>
                    </h4>
                    <button
                      onClick={() => onOpenThreeWayMatch(document)}
                      className="text-xs text-blue-600 hover:text-blue-800 font-semibold flex items-center space-x-1"
                    >
                      <span>Open 3-Way Match Studio</span>
                      <ArrowRight className="w-3 h-3" />
                    </button>
                  </div>

                  <div className="space-y-1 text-xs">
                    {document.threeWayMatch.details.map((detail, idx) => (
                      <div key={idx} className="flex items-start space-x-2 text-slate-700">
                        <span className="text-emerald-600 font-bold">&bull;</span>
                        <span>{detail}</span>
                      </div>
                    ))}
                  </div>

                  {document.threeWayMatch.status === 'QUANTITY_DISCREPANCY' && (
                    <div className="mt-3 pt-3 border-t border-slate-200 flex items-center justify-between">
                      <span className="text-xs text-amber-800 font-medium">
                        Short delivery discrepancy detected ({document.currency} $
                        {Math.abs(document.threeWayMatch.varianceAmount).toFixed(2)})
                      </span>
                      <button
                        onClick={() => setShowCreditNoteDraft(true)}
                        className="px-3 py-1 bg-amber-600 hover:bg-amber-500 text-white rounded text-xs font-semibold shadow-xs"
                      >
                        Draft Credit Note Request Email
                      </button>
                    </div>
                  )}
                </div>
              )}

              {/* Credit Note Request Email Draft Modal */}
              {showCreditNoteDraft && (
                <div className="p-4 bg-amber-50 border border-amber-300 rounded-xl space-y-2 text-xs">
                  <div className="flex items-center justify-between font-bold text-amber-900">
                    <span className="flex items-center space-x-1.5">
                      <Mail className="w-4 h-4 text-amber-600" />
                      <span>Draft Email to {document.vendor.name} Accounts</span>
                    </span>
                    <button
                      onClick={() => setShowCreditNoteDraft(false)}
                      className="text-amber-700 hover:text-amber-900"
                    >
                      Close
                    </button>
                  </div>
                  <div className="bg-white p-3 rounded border border-amber-200 font-mono text-[11px] text-slate-800 whitespace-pre-wrap">
{`To: ${document.vendor.contactEmail || 'accounts@' + document.vendor.name.toLowerCase().replace(/[^a-z0-9]/g, '') + '.com'}
Subject: Discrepancy Notification & Credit Note Request for Invoice #${document.documentNumber} (DO Ref: ${document.threeWayMatch?.matchedDoNumber || 'DO'})

Dear ${document.vendor.name} Accounts Team,

We have completed our receiving 3-way match audit for invoice #${document.documentNumber}.
Our warehouse receiving log indicates a short delivery discrepancy:
- Quantity Invoiced vs Received: Variance of SGD $${Math.abs(document.threeWayMatch?.varianceAmount || 130.8).toFixed(2)} (inclusive of 9% GST).

Could you please issue a Credit Note for this amount or provide an updated Tax Invoice so we can proceed with payment posting?

Thank you,
Accounts Payable Team`}
                  </div>
                  <p className="text-[11px] text-amber-700">
                    Saves SME AP officers 15-20 minutes drafting discrepancy notices.
                  </p>
                </div>
              )}
            </div>
          )}

          {/* TAB: VISUAL DOCUMENT PDF PREVIEW */}
          {activeTab === 'preview' && (
            <div className="space-y-4">
              <div className="flex items-center justify-between bg-slate-50 p-3 rounded-xl border border-slate-200 text-xs">
                <div className="flex items-center space-x-2">
                  <Printer className="w-4 h-4 text-emerald-600" />
                  <span className="font-bold text-slate-800">
                    Document Visual Paper: {document.fileName}
                  </span>
                  <span className="text-[10px] bg-slate-200 text-slate-700 px-2 py-0.5 rounded font-mono">
                    IRAS e-Tax Compliant
                  </span>
                </div>
                <div className="flex items-center space-x-2">
                  <button
                    onClick={handleDownloadPdf}
                    className="flex items-center space-x-1.5 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg font-bold shadow-xs transition"
                  >
                    <Download className="w-3.5 h-3.5" />
                    <span>Download / Print PDF</span>
                  </button>
                  {onOpenUpload && (
                    <button
                      onClick={onOpenUpload}
                      className="flex items-center space-x-1.5 px-3 py-1.5 bg-white border border-slate-300 hover:bg-slate-100 text-slate-700 rounded-lg font-semibold shadow-xs transition"
                    >
                      <Upload className="w-3.5 h-3.5" />
                      <span>Upload Another PDF</span>
                    </button>
                  )}
                </div>
              </div>

              {/* Realistic Printable Paper Document Container */}
              <div className="bg-white border border-slate-300 rounded-xl p-8 shadow-md max-w-2xl mx-auto space-y-6 text-slate-800 text-xs">
                {/* Header Letterhead */}
                <div className="flex justify-between items-start border-b border-slate-200 pb-5">
                  <div className="space-y-1">
                    <h2 className="text-lg font-black tracking-tight text-slate-900">
                      {document.vendor.name}
                    </h2>
                    <p className="text-slate-500 text-[11px] leading-tight">
                      {document.vendor.address || 'Singapore'}<br />
                      Singapore UEN: <strong className="text-slate-800">{document.vendor.uen || 'N/A'}</strong><br />
                      {document.vendor.gstRegNo && (
                        <span>GST Registration No: <strong className="text-slate-800">{document.vendor.gstRegNo}</strong></span>
                      )}
                    </p>
                  </div>
                  <div className="text-right space-y-1">
                    <span className="inline-block px-3 py-1 rounded bg-emerald-100 text-emerald-900 font-black text-xs uppercase tracking-wider border border-emerald-300">
                      {document.documentType.replace(/_/g, ' ')}
                    </span>
                    <div className="text-sm font-black text-slate-900 mt-1">
                      {document.documentNumber}
                    </div>
                    <div className="text-[11px] text-slate-500">
                      Date: <strong className="text-slate-700">{document.issueDate}</strong><br />
                      Due: <strong className="text-slate-700">{document.dueDate}</strong>
                    </div>
                  </div>
                </div>

                {/* Customer Details */}
                <div className="grid grid-cols-2 gap-4 bg-slate-50/70 p-3.5 rounded-lg border border-slate-100 text-[11px]">
                  <div>
                    <div className="font-bold uppercase text-slate-400 text-[10px]">Billed To:</div>
                    <div className="font-bold text-slate-900 text-xs mt-0.5">LionCity Trading &amp; Engineering Pte Ltd</div>
                    <div className="text-slate-600">UEN: 201809122K &bull; GST: M9-0091823-X</div>
                    <div className="text-slate-500">120 Robinson Road, #08-01, Singapore 068913</div>
                  </div>
                  <div>
                    <div className="font-bold uppercase text-slate-400 text-[10px]">Payment Terms &amp; Ref:</div>
                    <div className="text-slate-700 mt-0.5">Payment Terms: <strong>Net {document.paymentTermsDays || 30} Days</strong></div>
                    <div className="text-slate-700">Currency: <strong>{document.currency}</strong></div>
                    {document.referenceNumbers.length > 0 && (
                      <div className="text-slate-700">PO/DO Reference: <strong>{document.referenceNumbers.join(', ')}</strong></div>
                    )}
                  </div>
                </div>

                {/* Line Items Table */}
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="border-b-2 border-slate-300 bg-slate-50 text-slate-600 font-bold text-[10px] uppercase">
                      <th className="py-2 px-3">Item Description</th>
                      <th className="py-2 px-3 text-center">Qty</th>
                      <th className="py-2 px-3 text-right">Unit Price</th>
                      <th className="py-2 px-3 text-right">Total ({document.currency})</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-xs">
                    {document.lineItems.map((li, idx) => (
                      <tr key={idx}>
                        <td className="py-2.5 px-3">
                          <div className="font-semibold text-slate-900">{li.description}</div>
                          {li.itemCode && <div className="text-[10px] text-slate-400 font-mono">Code: {li.itemCode}</div>}
                        </td>
                        <td className="py-2.5 px-3 text-center font-mono">{li.quantity}</td>
                        <td className="py-2.5 px-3 text-right font-mono">${li.unitPrice.toFixed(2)}</td>
                        <td className="py-2.5 px-3 text-right font-mono font-bold text-slate-900">${li.amount.toFixed(2)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {/* Totals */}
                <div className="flex justify-end pt-2">
                  <div className="w-64 space-y-1.5 text-xs">
                    <div className="flex justify-between text-slate-600">
                      <span>Subtotal (Excl. GST):</span>
                      <span className="font-mono">${document.subtotal.toFixed(2)}</span>
                    </div>
                    <div className="flex justify-between text-slate-600">
                      <span>Singapore GST ({document.gstRate}%):</span>
                      <span className="font-mono">${document.gstAmount.toFixed(2)}</span>
                    </div>
                    <div className="flex justify-between font-black text-sm text-slate-900 border-t-2 border-slate-900 pt-2 pb-1">
                      <span>Total Payable:</span>
                      <span className="font-mono">SGD ${document.totalAmount.toFixed(2)}</span>
                    </div>
                  </div>
                </div>

                {/* Payment Box */}
                <div className="p-3.5 bg-slate-50 rounded-xl border border-slate-200 flex items-center justify-between text-[11px]">
                  <div className="space-y-0.5">
                    <div className="font-bold text-slate-900">🇸🇬 Singapore PayNow &amp; FAST Transfer</div>
                    <div>PayNow UEN: <strong className="text-emerald-700 font-mono">{document.vendor.payNowUen || document.vendor.uen || 'N/A'}</strong></div>
                    {document.vendor.bankName && (
                      <div className="text-slate-600">Bank: {document.vendor.bankName} | A/C: <span className="font-mono font-bold">{document.vendor.bankAccountNumber || 'N/A'}</span></div>
                    )}
                  </div>
                  <div className="w-16 h-16 bg-white border border-slate-300 rounded p-1 flex flex-col items-center justify-center text-center font-mono text-[8px] font-bold text-slate-900 shadow-2xs">
                    <div className="w-5 h-5 bg-slate-900 text-white flex items-center justify-center rounded-2xs text-[7px] mb-0.5">SG</div>
                    PayNow
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* TAB 2: LINE ITEMS */}
          {activeTab === 'lineItems' && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <p className="text-xs text-slate-600">
                  Extracted line items with auto-assigned Xero Chart of Accounts (COA) codes and Singapore 9% GST breakdown.
                </p>
              </div>

              <div className="border border-slate-200 rounded-xl overflow-hidden shadow-xs">
                <table className="w-full text-left text-xs">
                  <thead className="bg-slate-50 text-slate-600 uppercase font-semibold text-[10px] border-b border-slate-200">
                    <tr>
                      <th className="py-2.5 px-3">Description</th>
                      <th className="py-2.5 px-3 text-right">Qty</th>
                      <th className="py-2.5 px-3 text-right">Unit Price</th>
                      <th className="py-2.5 px-3 text-right">Line Total</th>
                      <th className="py-2.5 px-3">GST Rate</th>
                      <th className="py-2.5 px-3">Xero Account</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {document.lineItems.map((item, idx) => (
                      <tr key={item.id || idx} className="hover:bg-slate-50/50">
                        <td className="py-2.5 px-3 font-medium text-slate-800">
                          {item.description}
                          {item.itemCode && (
                            <div className="text-[10px] text-slate-400 font-mono">Code: {item.itemCode}</div>
                          )}
                        </td>
                        <td className="py-2.5 px-3 text-right font-mono">{item.quantity}</td>
                        <td className="py-2.5 px-3 text-right font-mono">
                          ${item.unitPrice.toFixed(2)}
                        </td>
                        <td className="py-2.5 px-3 text-right font-mono font-bold text-slate-900">
                          ${item.amount.toFixed(2)}
                        </td>
                        <td className="py-2.5 px-3">
                          <span className="px-1.5 py-0.5 rounded text-[10px] font-semibold bg-emerald-100 text-emerald-800">
                            {item.gstRate}% GST
                          </span>
                        </td>
                        <td className="py-2.5 px-3">
                          <span className="font-mono bg-slate-100 text-slate-700 px-2 py-0.5 rounded text-[10px]">
                            {item.xeroAccountCode} - {item.xeroAccountName}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* TAB 3: IRAS & MATH CHECKS */}
          {activeTab === 'checks' && (
            <div className="space-y-3">
              <div className="text-xs text-slate-600">
                Automated compliance rules and validation checks performed by Gemini AP analyzer:
              </div>

              <div className="grid grid-cols-1 gap-2.5">
                {document.validationChecks.map((chk) => (
                  <div
                    key={chk.id}
                    className={`p-3 rounded-xl border flex items-start space-x-3 text-xs ${
                      chk.status === 'PASSED'
                        ? 'bg-emerald-50/40 border-emerald-200'
                        : chk.status === 'WARNING'
                        ? 'bg-amber-50/40 border-amber-200'
                        : 'bg-rose-50/40 border-rose-200'
                    }`}
                  >
                    <div className="mt-0.5">
                      {chk.status === 'PASSED' ? (
                        <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                      ) : chk.status === 'WARNING' ? (
                        <AlertTriangle className="w-4 h-4 text-amber-600" />
                      ) : (
                        <XCircle className="w-4 h-4 text-rose-600" />
                      )}
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center space-x-2">
                        <span className="font-bold text-slate-900">{chk.title}</span>
                        <span className="text-[10px] uppercase font-semibold text-slate-400">
                          [{chk.category}]
                        </span>
                      </div>
                      <p className="text-slate-700 mt-0.5">{chk.description}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* TAB 4: PAYNOW SINGAPORE QR */}
          {activeTab === 'paynow' && (
            <div className="space-y-4">
              <div className="max-w-md mx-auto bg-slate-50 border border-slate-200 rounded-2xl p-6 text-center shadow-xs">
                <div className="inline-flex items-center space-x-1.5 px-3 py-1 rounded-full bg-purple-100 text-purple-800 text-xs font-bold mb-3">
                  <QrCode className="w-3.5 h-3.5" />
                  <span>Singapore PayNow Corporate</span>
                </div>

                <h4 className="text-sm font-bold text-slate-900">{document.vendor.name}</h4>
                <div className="text-2xl font-black text-slate-900 my-2">
                  SGD ${document.totalAmount.toFixed(2)}
                </div>

                {/* Simulated PayNow QR Code SVG */}
                <div className="my-4 p-4 bg-white rounded-xl border border-slate-200 inline-block shadow-sm">
                  <div className="w-48 h-48 bg-slate-950 p-2 rounded-lg flex items-center justify-center text-white text-center font-mono text-[10px]">
                    <div className="space-y-2">
                      <div className="w-12 h-12 border-2 border-emerald-400 mx-auto flex items-center justify-center font-bold text-emerald-400">
                        SGQR
                      </div>
                      <div className="text-[9px] text-slate-300">
                        PayNow UEN: {document.vendor.payNowUen || document.vendor.uen || 'UEN-NOT-SET'}
                      </div>
                      <div className="text-[9px] text-emerald-400 font-bold">
                        Ref: {document.documentNumber}
                      </div>
                      <div className="text-[8px] text-slate-400">Scan via DBS PayLah / OCBC / UOB</div>
                    </div>
                  </div>
                </div>

                <div className="flex items-center justify-center space-x-2 mt-2">
                  <button
                    onClick={handleCopyPayNow}
                    className="flex items-center space-x-1 px-3 py-1.5 bg-white border border-slate-300 rounded-lg text-xs font-semibold text-slate-700 hover:bg-slate-100 shadow-xs"
                  >
                    <Copy className="w-3.5 h-3.5 text-slate-500" />
                    <span>{copiedPayNow ? 'UEN Copied!' : 'Copy PayNow UEN'}</span>
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* TAB 5: XERO BILL PAYLOAD */}
          {activeTab === 'xero' && (
            <div className="space-y-4">
              <div className="flex items-center justify-between text-xs">
                <span className="text-slate-600">
                  Ready-to-sync Xero ACCPAY payload (conforms to Xero Invoices &amp; Bills API):
                </span>
                {document.xeroPostStatus.posted ? (
                  <span className="text-emerald-700 font-bold bg-emerald-100 px-2 py-0.5 rounded">
                    Status: Posted to Xero #{document.xeroPostStatus.xeroBillId}
                  </span>
                ) : (
                  <button
                    onClick={() => onPostToXero(document)}
                    className="flex items-center space-x-1.5 px-3 py-1 bg-emerald-600 hover:bg-emerald-500 text-white rounded font-bold text-xs shadow-xs"
                  >
                    <Send className="w-3 h-3" />
                    <span>Post Now</span>
                  </button>
                )}
              </div>

              <pre className="p-4 bg-slate-950 text-emerald-400 rounded-xl font-mono text-[11px] overflow-x-auto border border-slate-800 leading-relaxed">
{JSON.stringify(
  document.xeroPayload || {
    contact: { name: document.vendor.name },
    type: 'ACCPAY',
    invoiceNumber: document.documentNumber,
    date: document.issueDate,
    dueDate: document.dueDate,
    currencyCode: 'SGD',
    lineAmountTypes: 'Exclusive',
    lineItems: document.lineItems.map((li) => ({
      description: li.description,
      quantity: li.quantity,
      unitAmount: li.unitPrice,
      accountCode: li.xeroAccountCode,
      taxType: '9% GST on Expenses',
    })),
  },
  null,
  2
)}
              </pre>
            </div>
          )}
        </div>

        {/* Modal Bottom Footer */}
        <div className="p-4 bg-slate-50 border-t border-slate-200 flex items-center justify-between">
          <div className="text-xs text-slate-500">
            Source: <span className="font-semibold text-slate-700">{document.fileName}</span> &bull;{' '}
            {document.fileSizeFormatted || 'PDF'}
          </div>

          <div className="flex items-center space-x-2">
            <button
              onClick={onClose}
              className="px-4 py-2 bg-white border border-slate-300 text-slate-700 hover:bg-slate-100 rounded-lg text-xs font-semibold shadow-xs transition"
            >
              Close
            </button>

            {document.status !== 'APPROVED' && (
              <button
                onClick={() => {
                  onApproveDocument(document.id);
                  onClose();
                }}
                className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-bold shadow-xs transition"
              >
                Approve Invoice
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
