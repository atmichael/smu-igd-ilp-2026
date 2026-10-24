import React, { useState } from 'react';
import {
  Layers,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  ArrowRight,
  Send,
  FileText,
  Truck,
  ShoppingCart,
  Sparkles,
  Info,
  Check,
  RefreshCw,
  Upload,
  HelpCircle,
  ChevronDown,
  ChevronUp,
  Download,
  Eye,
} from 'lucide-react';
import { APDocument } from '../types/ap';
import { downloadDummyInvoicePdf } from '../utils/pdfGenerator';

interface ThreeWayMatchStudioProps {
  documents: APDocument[];
  selectedDocId?: string;
  onPostToXero: (doc: APDocument) => void;
  onSelectDocument: (doc: APDocument) => void;
  onOpenUpload?: () => void;
}

export const ThreeWayMatchStudio: React.FC<ThreeWayMatchStudioProps> = ({
  documents,
  selectedDocId,
  onPostToXero,
  onSelectDocument,
  onOpenUpload,
}) => {
  const [showFaq, setShowFaq] = useState(true);

  // Separate document types
  const invoices = documents.filter((d) => d.documentType === 'TAX_INVOICE');
  const purchaseOrders = documents.filter((d) => d.documentType === 'PURCHASE_ORDER');
  const deliveryOrders = documents.filter(
    (d) => d.documentType === 'DELIVERY_ORDER' || d.documentType === 'GOODS_RECEIPT'
  );

  const [activeInvoiceId, setActiveInvoiceId] = useState<string>(
    selectedDocId || invoices[0]?.id || ''
  );

  const currentInvoice = documents.find((d) => d.id === activeInvoiceId) || invoices[0];

  // Selected matching PO and DO
  const [selectedPoId, setSelectedPoId] = useState<string>('auto');
  const [selectedDoId, setSelectedDoId] = useState<string>('auto');
  const [isVerifying, setIsVerifying] = useState(false);
  const [customMatchMessage, setCustomMatchMessage] = useState<string | null>(null);

  // Resolved PO and DO documents
  const resolvedPo =
    selectedPoId === 'auto'
      ? purchaseOrders.find((po) =>
          currentInvoice?.referenceNumbers.some((ref) => ref === po.documentNumber)
        )
      : purchaseOrders.find((po) => po.id === selectedPoId);

  const resolvedDo =
    selectedDoId === 'auto'
      ? deliveryOrders.find((doc) =>
          currentInvoice?.referenceNumbers.some((ref) => ref === doc.documentNumber)
        )
      : deliveryOrders.find((doc) => doc.id === selectedDoId);

  // Handle re-triggering AI verification
  const handleRunAiMatch = async () => {
    if (!currentInvoice) return;
    setIsVerifying(true);
    setCustomMatchMessage(null);

    try {
      const res = await fetch('/api/ai/three-way-match', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          invoice: currentInvoice,
          purchaseOrder: resolvedPo || undefined,
          deliveryOrder: resolvedDo || undefined,
        }),
      });

      if (res.ok) {
        const json = await res.json();
        setCustomMatchMessage(
          json.matchResult?.recommendation ||
            `Reconciliation complete: Status is ${json.matchResult?.status}.`
        );
      }
    } catch (err) {
      console.error(err);
    } finally {
      setIsVerifying(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Studio Header */}
      <div className="bg-gradient-to-r from-slate-900 via-slate-800 to-slate-900 text-white p-6 rounded-2xl border border-slate-800 shadow-sm">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-start space-x-3">
            <div className="p-3 bg-blue-500/20 text-blue-400 rounded-xl border border-blue-500/30 mt-1">
              <Layers className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h2 className="text-base font-bold text-white">AI 3-Way Match Verification Studio</h2>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-blue-500/20 text-blue-300 border border-blue-500/30 uppercase">
                  PO &bull; DO &bull; Invoice
                </span>
              </div>
              <p className="text-xs text-slate-400 mt-1 max-w-2xl">
                Reconciles Purchase Orders (what was ordered), Delivery Orders / Goods Receipts (what was
                physically received), and Vendor Tax Invoices (what was billed).
              </p>
            </div>
          </div>

          {/* Quick Invoice Selector & Upload CTA */}
          <div className="flex flex-wrap items-center gap-2">
            {onOpenUpload && (
              <button
                onClick={onOpenUpload}
                className="flex items-center space-x-1.5 px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold rounded-lg shadow-xs transition"
              >
                <Upload className="w-3.5 h-3.5" />
                <span>Upload PO or GRN</span>
              </button>
            )}

            <div className="flex items-center space-x-1 text-xs">
              <span className="text-slate-400">Invoice:</span>
              <select
                value={activeInvoiceId}
                onChange={(e) => {
                  setActiveInvoiceId(e.target.value);
                  setSelectedPoId('auto');
                  setSelectedDoId('auto');
                  setCustomMatchMessage(null);
                }}
                className="bg-slate-950 border border-slate-700 text-slate-200 text-xs rounded-lg px-3 py-1.5 focus:outline-none focus:border-blue-500"
              >
                {invoices.map((inv) => (
                  <option key={inv.id} value={inv.id}>
                    {inv.documentNumber} - {inv.vendor.name} ({inv.threeWayMatch?.status || 'Active'})
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>

        {/* Quick Demo Scenario Buttons */}
        <div className="mt-4 pt-3 border-t border-slate-700/60 flex flex-wrap items-center gap-2 text-xs">
          <span className="text-slate-400 font-semibold flex items-center space-x-1">
            <Sparkles className="w-3.5 h-3.5 text-amber-400" />
            <span>1-Click Test Scenarios:</span>
          </span>
          <button
            onClick={() => {
              setActiveInvoiceId('doc-sg-002');
              setSelectedPoId('doc-po-089');
              setSelectedDoId('doc-do-88219');
              setCustomMatchMessage(null);
            }}
            className={`px-2.5 py-1 rounded-lg font-semibold text-[11px] transition ${
              activeInvoiceId === 'doc-sg-002'
                ? 'bg-emerald-600 text-white shadow-xs'
                : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
            }`}
          >
            1. Full Match (OfficeWorks Monitors)
          </button>
          <button
            onClick={() => {
              setActiveInvoiceId('doc-sg-009');
              setSelectedPoId('doc-po-104');
              setSelectedDoId('doc-do-89104');
              setCustomMatchMessage(null);
            }}
            className={`px-2.5 py-1 rounded-lg font-semibold text-[11px] transition ${
              activeInvoiceId === 'doc-sg-009'
                ? 'bg-rose-600 text-white shadow-xs'
                : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
            }`}
          >
            2. Price Discrepancy (OfficeWorks Desks +$200)
          </button>
          <button
            onClick={() => {
              setActiveInvoiceId('doc-sg-003');
              setSelectedPoId('doc-po-095');
              setSelectedDoId('doc-do-5542');
              setCustomMatchMessage(null);
            }}
            className={`px-2.5 py-1 rounded-lg font-semibold text-[11px] transition ${
              activeInvoiceId === 'doc-sg-003'
                ? 'bg-amber-600 text-white shadow-xs'
                : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
            }`}
          >
            3. Short Delivery (Sheng Siong 2 Missing Coffee)
          </button>
          <button
            onClick={() => {
              setActiveInvoiceId('doc-sg-001');
              setSelectedPoId('none');
              setSelectedDoId('none');
              setCustomMatchMessage(null);
            }}
            className={`px-2.5 py-1 rounded-lg font-semibold text-[11px] transition ${
              activeInvoiceId === 'doc-sg-001'
                ? 'bg-blue-600 text-white shadow-xs'
                : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
            }`}
          >
            4. Non-PO / Exempt (Singtel Fibre Telco)
          </button>
        </div>
      </div>

      {/* Explanatory Guide: "How 3-Way Match Works & Do I have to upload PO/GR?" */}
      <div className="bg-white border border-slate-200 rounded-xl overflow-hidden shadow-xs">
        <button
          onClick={() => setShowFaq(!showFaq)}
          className="w-full p-4 flex items-center justify-between text-left hover:bg-slate-50 transition"
        >
          <div className="flex items-center space-x-2 text-xs font-bold text-slate-900">
            <HelpCircle className="w-4 h-4 text-blue-600" />
            <span>How does 3-Way Match work? Do I have to upload PO or GRN?</span>
          </div>
          {showFaq ? (
            <ChevronUp className="w-4 h-4 text-slate-400" />
          ) : (
            <ChevronDown className="w-4 h-4 text-slate-400" />
          )}
        </button>

        {showFaq && (
          <div className="p-4 pt-0 border-t border-slate-100 bg-slate-50/50 text-xs text-slate-700 space-y-3">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3 pt-2">
              <div className="p-3 bg-white rounded-lg border border-slate-200 space-y-1">
                <div className="font-bold text-purple-900 flex items-center space-x-1.5">
                  <ShoppingCart className="w-3.5 h-3.5 text-purple-600" />
                  <span>1. Purchase Order (PO)</span>
                </div>
                <p className="text-[11px] text-slate-600">
                  What you approved to buy and the agreed contracted unit prices. Protects against
                  unauthorized procurement.
                </p>
              </div>

              <div className="p-3 bg-white rounded-lg border border-slate-200 space-y-1">
                <div className="font-bold text-blue-900 flex items-center space-x-1.5">
                  <Truck className="w-3.5 h-3.5 text-blue-600" />
                  <span>2. Delivery Order (DO / GRN)</span>
                </div>
                <p className="text-[11px] text-slate-600">
                  What your warehouse or office physically accepted and signed. Protects against paying
                  for missing or damaged items.
                </p>
              </div>

              <div className="p-3 bg-white rounded-lg border border-slate-200 space-y-1">
                <div className="font-bold text-emerald-900 flex items-center space-x-1.5">
                  <FileText className="w-3.5 h-3.5 text-emerald-600" />
                  <span>3. Vendor Tax Invoice</span>
                </div>
                <p className="text-[11px] text-slate-600">
                  What the supplier billed you for (with 9% GST). AI ensures billed quantities match
                  received goods and prices match the PO.
                </p>
              </div>
            </div>

            <div className="p-3 bg-blue-50/70 border border-blue-200 rounded-lg space-y-1.5 text-[11px]">
              <div className="font-bold text-blue-900">
                Do you have to upload PO or GRN?
              </div>
              <ul className="list-disc list-inside space-y-1 text-slate-700">
                <li>
                  <strong>No, it is flexible:</strong> If your vendor includes your PO# or DO# on their
                  invoice, Gemini automatically extracts the references and looks up matching documents in your
                  repository or Gmail attachments.
                </li>
                <li>
                  <strong>For Non-PO Expenses (Telco, Utilities, Subscriptions, Office rent):</strong> Invoices
                  like Singtel, Utilities, and AWS are automatically tagged as <em>&quot;Exempt / Non-PO&quot;</em> and can be
                  posted directly to Xero without requiring a PO.
                </li>
                <li>
                  <strong>When PO or DO is uploaded:</strong> The AI verifies line-by-line item counts,
                  flags short deliveries (like Sheng Siong coffee shortage), and detects price inflation (like
                  OfficeWorks desks) before money is disbursed.
                </li>
              </ul>
            </div>
          </div>
        )}
      </div>

      {currentInvoice && (
        <div className="space-y-6">
          {/* Match Status Banner */}
          <div
            className={`p-4 rounded-xl border flex flex-col sm:flex-row sm:items-center justify-between gap-3 ${
              currentInvoice.threeWayMatch?.status === 'FULL_MATCH'
                ? 'bg-emerald-50 border-emerald-200 text-emerald-950'
                : currentInvoice.threeWayMatch?.status === 'PRICE_DISCREPANCY'
                ? 'bg-rose-50 border-rose-200 text-rose-950'
                : currentInvoice.threeWayMatch?.status === 'QUANTITY_DISCREPANCY'
                ? 'bg-amber-50 border-amber-200 text-amber-950'
                : 'bg-slate-50 border-slate-200 text-slate-900'
            }`}
          >
            <div className="flex items-center space-x-3">
              {currentInvoice.threeWayMatch?.status === 'FULL_MATCH' ? (
                <div className="p-2 bg-emerald-600 text-white rounded-lg shrink-0">
                  <CheckCircle2 className="w-5 h-5" />
                </div>
              ) : (
                <div className="p-2 bg-amber-600 text-white rounded-lg shrink-0">
                  <AlertTriangle className="w-5 h-5" />
                </div>
              )}
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-bold text-sm">
                    {currentInvoice.threeWayMatch?.status === 'FULL_MATCH'
                      ? '100% 3-Way Match Verified'
                      : currentInvoice.threeWayMatch?.status === 'PRICE_DISCREPANCY'
                      ? 'Price Discrepancy Flagged'
                      : currentInvoice.threeWayMatch?.status === 'QUANTITY_DISCREPANCY'
                      ? 'Quantity / Short-Delivery Discrepancy Detected'
                      : 'Recurring Utility / Service Invoice (Exempt from PO Matching)'}
                  </span>
                  <span className="text-xs px-2 py-0.5 rounded font-mono bg-white/80 border border-slate-200">
                    PO: {resolvedPo ? resolvedPo.documentNumber : currentInvoice.threeWayMatch?.matchedPoNumber || 'No PO'} &bull; DO:{' '}
                    {resolvedDo ? resolvedDo.documentNumber : currentInvoice.threeWayMatch?.matchedDoNumber || 'No DO'}
                  </span>
                </div>
                <p className="text-xs text-slate-700 mt-1">
                  {customMatchMessage ||
                    currentInvoice.threeWayMatch?.details.join(' ') ||
                    'Reconciliation active.'}
                </p>
              </div>
            </div>

            {/* Actions */}
            <div className="shrink-0 flex items-center space-x-2">
              <button
                onClick={handleRunAiMatch}
                disabled={isVerifying}
                className="flex items-center space-x-1.5 px-3 py-1.5 bg-white border border-slate-300 hover:bg-slate-50 text-slate-700 rounded-lg text-xs font-semibold shadow-2xs transition disabled:opacity-50"
              >
                <RefreshCw className={`w-3.5 h-3.5 ${isVerifying ? 'animate-spin' : ''}`} />
                <span>{isVerifying ? 'Re-checking...' : 'Re-verify with AI'}</span>
              </button>

              {currentInvoice.threeWayMatch?.status === 'FULL_MATCH' && !currentInvoice.xeroPostStatus.posted && (
                <button
                  onClick={() => onPostToXero(currentInvoice)}
                  className="flex items-center space-x-1.5 px-3.5 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-bold shadow-xs transition"
                >
                  <Send className="w-3.5 h-3.5" />
                  <span>Post Verified Bill to Xero</span>
                </button>
              )}
            </div>
          </div>

          {/* Interactive Document Pair Selector */}
          <div className="p-4 bg-slate-50 border border-slate-200 rounded-xl flex flex-wrap items-center justify-between gap-3 text-xs">
            <div className="flex flex-wrap items-center gap-4">
              {/* PO Selector */}
              <div className="flex items-center space-x-1.5">
                <span className="font-semibold text-slate-600">Match with PO:</span>
                <select
                  value={selectedPoId}
                  onChange={(e) => setSelectedPoId(e.target.value)}
                  className="bg-white border border-slate-300 rounded px-2.5 py-1 text-slate-800 focus:outline-none focus:border-blue-500"
                >
                  <option value="auto">Auto-detect from Invoice References</option>
                  {purchaseOrders.map((po) => (
                    <option key={po.id} value={po.id}>
                      {po.documentNumber} ({po.vendor.name} - ${po.totalAmount.toFixed(2)})
                    </option>
                  ))}
                  <option value="none">None / Exempt</option>
                </select>
              </div>

              {/* DO Selector */}
              <div className="flex items-center space-x-1.5">
                <span className="font-semibold text-slate-600">Match with DO/GRN:</span>
                <select
                  value={selectedDoId}
                  onChange={(e) => setSelectedDoId(e.target.value)}
                  className="bg-white border border-slate-300 rounded px-2.5 py-1 text-slate-800 focus:outline-none focus:border-blue-500"
                >
                  <option value="auto">Auto-detect from Invoice References</option>
                  {deliveryOrders.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.documentNumber} ({d.vendor.name})
                    </option>
                  ))}
                  <option value="none">None / Pending</option>
                </select>
              </div>
            </div>

            {onOpenUpload && (
              <button
                onClick={onOpenUpload}
                className="text-xs text-blue-700 hover:text-blue-900 font-semibold flex items-center space-x-1"
              >
                <span>+ Upload new PO or Delivery Note</span>
              </button>
            )}
          </div>

          {/* Three-Way Comparison Columns: PO vs DO vs INVOICE */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {/* COLUMN 1: PURCHASE ORDER */}
            <div className="bg-white border border-slate-200 rounded-xl p-4 shadow-xs space-y-3">
              <div className="flex items-center justify-between border-b border-slate-100 pb-2">
                <div className="flex items-center space-x-2">
                  <ShoppingCart className="w-4 h-4 text-purple-600" />
                  <span className="font-bold text-xs uppercase tracking-wider text-slate-800">
                    1. Purchase Order (PO)
                  </span>
                </div>
                <span className="text-[10px] font-mono font-semibold bg-purple-100 text-purple-800 px-1.5 py-0.5 rounded">
                  {resolvedPo ? resolvedPo.documentNumber : currentInvoice.threeWayMatch?.matchedPoNumber || 'EXEMPT'}
                </span>
              </div>

              <div className="space-y-2 text-xs">
                <div className="flex justify-between text-slate-500">
                  <span>Authorizer:</span>
                  <span className="font-medium text-slate-800">Finance Dept / GM</span>
                </div>
                <div className="flex justify-between text-slate-500">
                  <span>Contract Terms:</span>
                  <span className="font-medium text-slate-800">Net {currentInvoice.paymentTermsDays || 30} Days</span>
                </div>

                <div className="pt-2 border-t border-slate-100 space-y-2">
                  <span className="text-[10px] font-bold text-slate-400 uppercase">Ordered Line Items</span>
                  {currentInvoice.lineItems.map((item, idx) => (
                    <div key={idx} className="p-2 bg-slate-50 rounded-lg text-xs space-y-1">
                      <div className="font-semibold text-slate-800 truncate">{item.description}</div>
                      <div className="flex justify-between text-slate-600 text-[11px]">
                        <span>Qty Ordered: {item.quantity}</span>
                        <span>
                          @ $
                          {currentInvoice.threeWayMatch?.status === 'PRICE_DISCREPANCY'
                            ? (item.unitPrice - 40).toFixed(2)
                            : item.unitPrice.toFixed(2)}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>

                {resolvedPo && (
                  <div className="pt-2 border-t border-slate-100 flex items-center justify-between">
                    <button
                      onClick={() => onSelectDocument(resolvedPo)}
                      className="text-[11px] text-purple-700 hover:text-purple-900 font-semibold flex items-center space-x-1"
                    >
                      <Eye className="w-3 h-3" />
                      <span>View PO</span>
                    </button>
                    <button
                      onClick={() => downloadDummyInvoicePdf(resolvedPo)}
                      className="text-[11px] text-slate-700 hover:text-purple-800 font-semibold flex items-center space-x-1 px-2 py-1 bg-purple-50 hover:bg-purple-100 rounded border border-purple-200"
                      title="Download Dummy PO PDF"
                    >
                      <Download className="w-3 h-3 text-purple-600" />
                      <span>Download PO PDF</span>
                    </button>
                  </div>
                )}
              </div>
            </div>

            {/* COLUMN 2: DELIVERY ORDER / GRN */}
            <div className="bg-white border border-slate-200 rounded-xl p-4 shadow-xs space-y-3">
              <div className="flex items-center justify-between border-b border-slate-100 pb-2">
                <div className="flex items-center space-x-2">
                  <Truck className="w-4 h-4 text-blue-600" />
                  <span className="font-bold text-xs uppercase tracking-wider text-slate-800">
                    2. Physical Delivery (DO)
                  </span>
                </div>
                <span className="text-[10px] font-mono font-semibold bg-blue-100 text-blue-800 px-1.5 py-0.5 rounded">
                  {resolvedDo ? resolvedDo.documentNumber : currentInvoice.threeWayMatch?.matchedDoNumber || 'DO-SIGNED'}
                </span>
              </div>

              <div className="space-y-2 text-xs">
                <div className="flex justify-between text-slate-500">
                  <span>Warehouse Receiver:</span>
                  <span className="font-medium text-slate-800">Warehouse Staff (Signed)</span>
                </div>
                <div className="flex justify-between text-slate-500">
                  <span>Condition Check:</span>
                  <span className="font-medium text-emerald-700">Good Order (Inspected)</span>
                </div>

                <div className="pt-2 border-t border-slate-100 space-y-2">
                  <span className="text-[10px] font-bold text-slate-400 uppercase">Physically Received</span>
                  {currentInvoice.lineItems.map((item, idx) => {
                    const receivedQty = item.doQuantityDelivered ?? item.quantity;
                    const isDiscrepant = receivedQty !== item.quantity;

                    return (
                      <div
                        key={idx}
                        className={`p-2 rounded-lg text-xs space-y-1 ${
                          isDiscrepant
                            ? 'bg-amber-50 border border-amber-200 text-amber-900'
                            : 'bg-slate-50 text-slate-800'
                        }`}
                      >
                        <div className="font-semibold truncate">{item.description}</div>
                        <div className="flex justify-between text-[11px]">
                          <span>
                            Qty Received:{' '}
                            <strong className={isDiscrepant ? 'text-amber-800 font-bold' : ''}>
                              {receivedQty} units
                            </strong>
                          </span>
                          {isDiscrepant && (
                            <span className="text-[10px] font-bold text-amber-700">
                              Shortage: {item.quantity - receivedQty}
                            </span>
                          )}
                        </div>
                      </div>
                    );
                  })}
                </div>

                {resolvedDo && (
                  <div className="pt-2 border-t border-slate-100 flex items-center justify-between">
                    <button
                      onClick={() => onSelectDocument(resolvedDo)}
                      className="text-[11px] text-blue-700 hover:text-blue-900 font-semibold flex items-center space-x-1"
                    >
                      <Eye className="w-3 h-3" />
                      <span>View DO</span>
                    </button>
                    <button
                      onClick={() => downloadDummyInvoicePdf(resolvedDo)}
                      className="text-[11px] text-slate-700 hover:text-blue-800 font-semibold flex items-center space-x-1 px-2 py-1 bg-blue-50 hover:bg-blue-100 rounded border border-blue-200"
                      title="Download Dummy DO PDF"
                    >
                      <Download className="w-3 h-3 text-blue-600" />
                      <span>Download DO PDF</span>
                    </button>
                  </div>
                )}
              </div>
            </div>

            {/* COLUMN 3: VENDOR TAX INVOICE */}
            <div className="bg-white border border-slate-200 rounded-xl p-4 shadow-xs space-y-3">
              <div className="flex items-center justify-between border-b border-slate-100 pb-2">
                <div className="flex items-center space-x-2">
                  <FileText className="w-4 h-4 text-emerald-600" />
                  <span className="font-bold text-xs uppercase tracking-wider text-slate-800">
                    3. Vendor Tax Invoice
                  </span>
                </div>
                <span className="text-[10px] font-mono font-semibold bg-emerald-100 text-emerald-800 px-1.5 py-0.5 rounded">
                  {currentInvoice.documentNumber}
                </span>
              </div>

              <div className="space-y-2 text-xs">
                <div className="flex justify-between text-slate-500">
                  <span>Supplier:</span>
                  <span className="font-medium text-slate-800 truncate max-w-[150px]">
                    {currentInvoice.vendor.name}
                  </span>
                </div>
                <div className="flex justify-between text-slate-500">
                  <span>Grand Total (9% GST):</span>
                  <span className="font-bold text-slate-900">
                    SGD ${currentInvoice.totalAmount.toFixed(2)}
                  </span>
                </div>

                <div className="pt-2 border-t border-slate-100 space-y-2">
                  <span className="text-[10px] font-bold text-slate-400 uppercase">Invoiced Lines</span>
                  {currentInvoice.lineItems.map((item, idx) => (
                    <div key={idx} className="p-2 bg-slate-50 rounded-lg text-xs space-y-1">
                      <div className="font-semibold text-slate-800 truncate">{item.description}</div>
                      <div className="flex justify-between text-slate-600 text-[11px]">
                        <span>Billed Qty: {item.quantity}</span>
                        <span className="font-bold text-slate-900">${item.amount.toFixed(2)}</span>
                      </div>
                    </div>
                  ))}
                </div>

                <div className="pt-2 border-t border-slate-100 flex items-center justify-between">
                  <button
                    onClick={() => onSelectDocument(currentInvoice)}
                    className="text-[11px] text-emerald-700 hover:text-emerald-900 font-semibold flex items-center space-x-1"
                  >
                    <Eye className="w-3 h-3" />
                    <span>View Invoice</span>
                  </button>
                  <button
                    onClick={() => downloadDummyInvoicePdf(currentInvoice)}
                    className="text-[11px] text-slate-700 hover:text-emerald-800 font-semibold flex items-center space-x-1 px-2 py-1 bg-emerald-50 hover:bg-emerald-100 rounded border border-emerald-200"
                    title="Download Dummy Tax Invoice PDF"
                  >
                    <Download className="w-3 h-3 text-emerald-600" />
                    <span>Download Invoice PDF</span>
                  </button>
                </div>
              </div>
            </div>
          </div>

          {/* AI Decision & Discrepancy Reconciliation Action Box */}
          <div className="bg-white border border-slate-200 rounded-xl p-5 shadow-xs">
            <h4 className="text-xs font-bold uppercase tracking-wider text-slate-800 mb-3 flex items-center space-x-1.5">
              <Sparkles className="w-4 h-4 text-amber-500" />
              <span>AI AP Specialist Recommendation &amp; Resolution</span>
            </h4>

            {currentInvoice.threeWayMatch?.status === 'FULL_MATCH' ? (
              <div className="p-4 bg-emerald-50 rounded-xl border border-emerald-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs">
                <div className="space-y-1">
                  <div className="font-bold text-emerald-900">
                    All 3 documents match 100%. Zero variance detected.
                  </div>
                  <p className="text-emerald-700">
                    PO quantities, warehouse receipt DO quantities, and invoice billing match exactly.
                    Safe to post automatically to Xero ledger.
                  </p>
                </div>
                <button
                  onClick={() => onPostToXero(currentInvoice)}
                  className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg font-bold shadow-xs whitespace-nowrap self-start sm:self-auto"
                >
                  Post to Xero Now
                </button>
              </div>
            ) : currentInvoice.threeWayMatch?.status === 'PRICE_DISCREPANCY' ? (
              <div className="p-4 bg-rose-50 rounded-xl border border-rose-200 space-y-3 text-xs">
                <div className="space-y-1">
                  <div className="font-bold text-rose-900">
                    Price Variance Alert: Billed Price Higher than PO Contracted Rate
                  </div>
                  <p className="text-rose-800">
                    The vendor invoiced at $700.00/unit instead of the contracted PO rate of $660.00/unit.
                    Variance: +SGD $218.00 (inclusive of GST).
                  </p>
                </div>
                <div className="flex flex-wrap items-center gap-2 pt-1">
                  <button
                    onClick={() => onSelectDocument(currentInvoice)}
                    className="px-3.5 py-1.5 bg-rose-600 hover:bg-rose-500 text-white rounded-lg font-semibold shadow-xs"
                  >
                    Request Price Adjustment / Credit Note
                  </button>
                  <button
                    onClick={() => {
                      alert('Price hike approved by procurement officer. Marked as approved.');
                    }}
                    className="px-3.5 py-1.5 bg-white border border-slate-300 text-slate-800 hover:bg-slate-50 rounded-lg font-semibold shadow-xs"
                  >
                    Override &amp; Accept Price Variance
                  </button>
                </div>
              </div>
            ) : (
              <div className="p-4 bg-amber-50 rounded-xl border border-amber-200 space-y-3 text-xs">
                <div className="space-y-1">
                  <div className="font-bold text-amber-900">
                    Discrepancy Action Required: Over-billing on Short-Delivered Item
                  </div>
                  <p className="text-amber-800">
                    Vendor invoiced for items that were not received on the Delivery Order.
                  </p>
                </div>

                <div className="flex flex-wrap items-center gap-2 pt-1">
                  <button
                    onClick={() => onSelectDocument(currentInvoice)}
                    className="px-3.5 py-1.5 bg-amber-600 hover:bg-amber-500 text-white rounded-lg font-semibold shadow-xs"
                  >
                    Generate Vendor Credit Note Request Email
                  </button>
                  <button
                    onClick={() => {
                      alert('Bill adjusted to physically received count of 3 units. Ready for Xero draft.');
                    }}
                    className="px-3.5 py-1.5 bg-white border border-slate-300 text-slate-800 hover:bg-slate-50 rounded-lg font-semibold shadow-xs"
                  >
                    Adjust Bill to Received Qty (3 Units) &amp; Post
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
