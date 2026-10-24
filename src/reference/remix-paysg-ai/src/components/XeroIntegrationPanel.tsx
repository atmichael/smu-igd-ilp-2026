import React, { useState } from 'react';
import {
  RefreshCw,
  CheckCircle2,
  ExternalLink,
  Send,
  Layers,
  Building,
  Settings,
  ShieldCheck,
  Check,
  FileText,
  DollarSign,
} from 'lucide-react';
import { APDocument } from '../types/ap';

interface XeroIntegrationPanelProps {
  documents: APDocument[];
  onBatchPost: (docIds: string[]) => void;
  onPostSingle: (doc: APDocument) => void;
  onSelectDocument: (doc: APDocument) => void;
}

export const XeroIntegrationPanel: React.FC<XeroIntegrationPanelProps> = ({
  documents,
  onBatchPost,
  onPostSingle,
  onSelectDocument,
}) => {
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [isPosting, setIsPosting] = useState(false);
  const [showSettings, setShowSettings] = useState(false);

  // Invoices ready for Xero posting (approved and unposted)
  const readyInvoices = documents.filter(
    (d) => d.documentType === 'TAX_INVOICE' && !d.xeroPostStatus.posted
  );

  // Invoices already posted
  const postedInvoices = documents.filter((d) => d.xeroPostStatus.posted);

  const handleToggleSelect = (id: string) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
    );
  };

  const handleSelectAll = () => {
    if (selectedIds.length === readyInvoices.length) {
      setSelectedIds([]);
    } else {
      setSelectedIds(readyInvoices.map((d) => d.id));
    }
  };

  const handleRunBatchPost = async () => {
    if (selectedIds.length === 0) return;
    setIsPosting(true);
    await onBatchPost(selectedIds);
    setSelectedIds([]);
    setIsPosting(false);
  };

  return (
    <div className="space-y-6">
      {/* Xero Connection Banner */}
      <div className="bg-gradient-to-r from-sky-900 via-slate-900 to-sky-950 text-white p-6 rounded-2xl border border-sky-800 shadow-sm">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-start space-x-3">
            <div className="p-3 bg-sky-500/20 text-sky-400 rounded-xl border border-sky-500/30 mt-1">
              <RefreshCw className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h2 className="text-base font-bold text-white">Xero Accounting Integration (Singapore)</h2>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 flex items-center space-x-1">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-400"></span>
                  <span>Connected: LionCity Trading Pte Ltd</span>
                </span>
              </div>
              <p className="text-xs text-sky-200/80 mt-1 max-w-2xl">
                Automatically maps line items to Xero Chart of Accounts (COA) codes and Singapore IRAS 9%
                GST tax types. Pushes bills as <code className="text-sky-300 font-mono">ACCPAY</code> Invoices
                directly into Xero for SME payment runs.
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-2 self-start md:self-auto">
            <button
              onClick={() => setShowSettings(!showSettings)}
              className="px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 rounded-lg text-xs font-semibold flex items-center space-x-1.5 transition"
            >
              <Settings className="w-3.5 h-3.5" />
              <span>{showSettings ? 'Hide COA Rules' : 'View Account Mapping'}</span>
            </button>
          </div>
        </div>
      </div>

      {/* COA and Tax Mapping Details (collapsible) */}
      {showSettings && (
        <div className="bg-white border border-slate-200 rounded-xl p-5 shadow-xs space-y-4 text-xs animate-in fade-in duration-150">
          <h4 className="font-bold text-slate-900 text-sm">Singapore SME Chart of Accounts (COA) Mapping Rules</h4>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <span className="text-[10px] font-bold uppercase text-slate-400">Default Expense Accounts</span>
              <div className="p-3 bg-slate-50 rounded-lg space-y-1.5 font-mono text-[11px]">
                <div className="flex justify-between">
                  <span className="text-slate-700">400 - Cost of Goods Sold</span>
                  <span className="text-emerald-700 font-semibold">9% GST on Expenses</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-700">429 - General &amp; Pantry Supplies</span>
                  <span className="text-emerald-700 font-semibold">9% GST on Expenses</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-700">445 - Telephone &amp; Internet</span>
                  <span className="text-emerald-700 font-semibold">9% GST on Expenses</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-700">473 - Repairs &amp; Maintenance</span>
                  <span className="text-emerald-700 font-semibold">9% GST on Expenses</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-700">720 - Office Equipment (Fixed Asset)</span>
                  <span className="text-emerald-700 font-semibold">9% GST on Capital</span>
                </div>
              </div>
            </div>

            <div className="space-y-2">
              <span className="text-[10px] font-bold uppercase text-slate-400">IRAS GST Tax Authority Settings</span>
              <div className="p-3 bg-slate-50 rounded-lg space-y-1.5 text-[11px] text-slate-700">
                <div>
                  <strong className="text-slate-900">Standard Rate:</strong> 9.0% Goods &amp; Services Tax
                </div>
                <div>
                  <strong className="text-slate-900">IRAS Form 5 Box 5:</strong> Taxable Purchases Base Value
                </div>
                <div>
                  <strong className="text-slate-900">IRAS Form 5 Box 7:</strong> Input Tax and Refunds Claimed
                </div>
                <div>
                  <strong className="text-slate-900">Post Status Default:</strong> Draft Bill (Requires AP Manager approval in Xero)
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* SECTION 1: Ready to Post Queue */}
      <div className="bg-white border border-slate-200 rounded-xl p-5 shadow-xs space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 pb-3">
          <div>
            <h3 className="font-bold text-sm text-slate-900 flex items-center space-x-2">
              <FileText className="w-4 h-4 text-emerald-600" />
              <span>Invoices Ready for Xero Sync ({readyInvoices.length})</span>
            </h3>
            <p className="text-xs text-slate-500">
              Approved vendor invoices ready to create ACCPAY Bills in Xero.
            </p>
          </div>

          <div className="flex items-center space-x-2">
            <button
              onClick={handleSelectAll}
              className="text-xs text-slate-600 hover:text-slate-900 font-semibold px-2.5 py-1.5 border border-slate-200 rounded-lg"
            >
              {selectedIds.length === readyInvoices.length && readyInvoices.length > 0
                ? 'Deselect All'
                : 'Select All'}
            </button>

            <button
              onClick={handleRunBatchPost}
              disabled={selectedIds.length === 0 || isPosting}
              className="flex items-center space-x-1.5 px-4 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-bold shadow-xs transition disabled:opacity-50"
            >
              <Send className="w-3.5 h-3.5" />
              <span>
                {isPosting ? 'Posting...' : `Post Selected to Xero (${selectedIds.length})`}
              </span>
            </button>
          </div>
        </div>

        {readyInvoices.length === 0 ? (
          <div className="py-8 text-center text-slate-400 text-xs">
            <CheckCircle2 className="w-8 h-8 mx-auto mb-2 text-emerald-500/60" />
            All approved invoices have been posted to Xero!
          </div>
        ) : (
          <div className="divide-y divide-slate-100 text-xs">
            {readyInvoices.map((inv) => (
              <div
                key={inv.id}
                className="py-3 flex items-center justify-between hover:bg-slate-50/60 px-2 rounded-lg transition"
              >
                <div className="flex items-center space-x-3">
                  <input
                    type="checkbox"
                    checked={selectedIds.includes(inv.id)}
                    onChange={() => handleToggleSelect(inv.id)}
                    className="w-4 h-4 text-emerald-600 rounded border-slate-300 focus:ring-emerald-500"
                  />
                  <div>
                    <div className="flex items-center space-x-2">
                      <span className="font-bold text-slate-900">{inv.documentNumber}</span>
                      <span className="text-slate-400">&bull;</span>
                      <span className="font-semibold text-slate-700">{inv.vendor.name}</span>
                      {inv.vendor.uen && (
                        <span className="text-[10px] text-slate-500 font-mono">({inv.vendor.uen})</span>
                      )}
                    </div>
                    <div className="text-[11px] text-slate-500">
                      Due: {inv.dueDate} &bull; {inv.lineItems.length} line items &bull; 9% GST: $
                      {inv.gstAmount.toFixed(2)}
                    </div>
                  </div>
                </div>

                <div className="flex items-center space-x-3">
                  <span className="font-bold text-slate-900 text-sm">
                    SGD ${inv.totalAmount.toFixed(2)}
                  </span>
                  <button
                    onClick={() => onPostSingle(inv)}
                    className="px-3 py-1 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded text-xs font-semibold transition"
                  >
                    Post 1-Click
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* SECTION 2: Synced Bills History */}
      <div className="bg-white border border-slate-200 rounded-xl p-5 shadow-xs space-y-4">
        <h3 className="font-bold text-sm text-slate-900 flex items-center space-x-2">
          <CheckCircle2 className="w-4 h-4 text-sky-600" />
          <span>Synced Xero Bills Log ({postedInvoices.length})</span>
        </h3>

        {postedInvoices.length === 0 ? (
          <div className="py-6 text-center text-slate-400 text-xs">
            No bills posted to Xero yet. Select invoices above to test posting!
          </div>
        ) : (
          <div className="divide-y divide-slate-100 text-xs">
            {postedInvoices.map((inv) => (
              <div
                key={inv.id}
                className="py-3 flex items-center justify-between hover:bg-slate-50/60 px-2 rounded-lg transition"
              >
                <div className="space-y-0.5">
                  <div className="flex items-center space-x-2">
                    <span className="font-bold text-slate-900">{inv.documentNumber}</span>
                    <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-sky-100 text-sky-800 font-semibold">
                      {inv.xeroPostStatus.xeroBillId || 'XERO-SYNCED'}
                    </span>
                    <span className="font-semibold text-slate-700">{inv.vendor.name}</span>
                  </div>
                  <div className="text-[11px] text-slate-400">
                    Posted at: {inv.xeroPostStatus.postedAt || 'Just now'} &bull; Status: ACCPAY Draft in
                    Xero
                  </div>
                </div>

                <div className="flex items-center space-x-3">
                  <span className="font-bold text-slate-900 text-sm">
                    SGD ${inv.totalAmount.toFixed(2)}
                  </span>
                  <button
                    onClick={() => onSelectDocument(inv)}
                    className="p-1.5 text-slate-500 hover:text-slate-800 hover:bg-slate-100 rounded"
                    title="View Document Details"
                  >
                    <ExternalLink className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
