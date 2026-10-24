import React, { useState } from 'react';
import {
  FileText,
  Search,
  Filter,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  Eye,
  Layers,
  ArrowUpDown,
  RefreshCw,
  QrCode,
  Building,
  Calendar,
  DollarSign,
  Send,
  Download,
} from 'lucide-react';
import { APDocument, DocumentType, PostingVerdict } from '../types/ap';
import { downloadDummyInvoicePdf } from '../utils/pdfGenerator';

interface DocumentListProps {
  documents: APDocument[];
  onSelectDocument: (doc: APDocument) => void;
  onPostToXero: (doc: APDocument) => void;
  onOpenThreeWayMatch: (doc: APDocument) => void;
}

export const DocumentList: React.FC<DocumentListProps> = ({
  documents,
  onSelectDocument,
  onPostToXero,
  onOpenThreeWayMatch,
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedType, setSelectedType] = useState<string>('ALL');
  const [selectedVerdict, setSelectedVerdict] = useState<string>('ALL');

  // Filter documents
  const filteredDocs = documents.filter((doc) => {
    const matchesSearch =
      doc.documentNumber.toLowerCase().includes(searchTerm.toLowerCase()) ||
      doc.vendor.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (doc.vendor.uen && doc.vendor.uen.toLowerCase().includes(searchTerm.toLowerCase())) ||
      doc.referenceNumbers.some((ref) => ref.toLowerCase().includes(searchTerm.toLowerCase()));

    const matchesType = selectedType === 'ALL' || doc.documentType === selectedType;
    const matchesVerdict = selectedVerdict === 'ALL' || doc.postingVerdict === selectedVerdict;

    return matchesSearch && matchesType && matchesVerdict;
  });

  const getDocTypeBadge = (type: DocumentType) => {
    switch (type) {
      case 'TAX_INVOICE':
        return (
          <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-emerald-100 text-emerald-800 border border-emerald-200">
            Tax Invoice
          </span>
        );
      case 'DELIVERY_ORDER':
        return (
          <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-blue-100 text-blue-800 border border-blue-200">
            Delivery Order (DO)
          </span>
        );
      case 'STATEMENT_OF_ACCOUNT':
        return (
          <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-purple-100 text-purple-800 border border-purple-200">
            Statement of Account (SOA)
          </span>
        );
      case 'CREDIT_NOTE':
        return (
          <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-amber-100 text-amber-800 border border-amber-200">
            Credit Note (CN)
          </span>
        );
      case 'GOODS_RECEIPT':
        return (
          <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-teal-100 text-teal-800 border border-teal-200">
            Goods Receipt (GRN)
          </span>
        );
      case 'PURCHASE_ORDER':
        return (
          <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-slate-100 text-slate-800 border border-slate-200">
            Purchase Order (PO)
          </span>
        );
      default:
        return (
          <span className="px-2 py-0.5 rounded text-[11px] font-medium bg-gray-100 text-gray-800">
            {type}
          </span>
        );
    }
  };

  const getVerdictBadge = (verdict: PostingVerdict) => {
    switch (verdict) {
      case 'APPROVED':
        return (
          <span className="inline-flex items-center space-x-1 px-2 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
            <CheckCircle2 className="w-3 h-3 text-emerald-600" />
            <span>Ready to Post</span>
          </span>
        );
      case 'REQUIRES_ATTENTION':
        return (
          <span className="inline-flex items-center space-x-1 px-2 py-0.5 rounded-full text-[11px] font-semibold bg-amber-100 text-amber-800 border border-amber-200">
            <AlertTriangle className="w-3 h-3 text-amber-600" />
            <span>Review Flagged</span>
          </span>
        );
      case 'CRITICAL_BLOCKER':
        return (
          <span className="inline-flex items-center space-x-1 px-2 py-0.5 rounded-full text-[11px] font-semibold bg-rose-100 text-rose-800 border border-rose-200">
            <XCircle className="w-3 h-3 text-rose-600" />
            <span>Posting Blocked</span>
          </span>
        );
    }
  };

  const getMatchBadge = (doc: APDocument) => {
    if (!doc.threeWayMatch || doc.threeWayMatch.status === 'NOT_APPLICABLE') {
      return <span className="text-[11px] text-slate-400 font-mono">N/A (Non-PO)</span>;
    }
    switch (doc.threeWayMatch.status) {
      case 'FULL_MATCH':
        return (
          <span className="inline-flex items-center space-x-1 px-1.5 py-0.5 rounded text-[10px] font-bold bg-teal-100 text-teal-800">
            <CheckCircle2 className="w-3 h-3 text-teal-600" />
            <span>3-Way Match OK</span>
          </span>
        );
      case 'QUANTITY_DISCREPANCY':
        return (
          <span className="inline-flex items-center space-x-1 px-1.5 py-0.5 rounded text-[10px] font-bold bg-amber-100 text-amber-800">
            <AlertTriangle className="w-3 h-3 text-amber-600" />
            <span>Qty Variance</span>
          </span>
        );
      case 'PRICE_DISCREPANCY':
        return (
          <span className="inline-flex items-center space-x-1 px-1.5 py-0.5 rounded text-[10px] font-bold bg-rose-100 text-rose-800">
            <AlertTriangle className="w-3 h-3 text-rose-600" />
            <span>Price Variance</span>
          </span>
        );
      default:
        return (
          <span className="px-1.5 py-0.5 rounded text-[10px] font-medium bg-slate-100 text-slate-700">
            Pending Match
          </span>
        );
    }
  };

  return (
    <div className="space-y-4">
      {/* Search and Filters Bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-3">
        <div className="relative flex-1 max-w-md">
          <Search className="w-4 h-4 absolute left-3 top-2.5 text-slate-400" />
          <input
            type="text"
            placeholder="Search vendor name, Singapore UEN, invoice #, PO #..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-9 pr-4 py-1.5 text-xs border border-slate-300 rounded-lg focus:outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 text-slate-800"
          />
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {/* Document Type Filter */}
          <div className="flex items-center space-x-1 text-xs">
            <Filter className="w-3.5 h-3.5 text-slate-400" />
            <select
              value={selectedType}
              onChange={(e) => setSelectedType(e.target.value)}
              className="border border-slate-300 rounded-lg px-2.5 py-1.5 text-xs text-slate-700 bg-white focus:outline-none focus:border-emerald-500"
            >
              <option value="ALL">All Document Types</option>
              <option value="TAX_INVOICE">Tax Invoices</option>
              <option value="DELIVERY_ORDER">Delivery Orders (DO)</option>
              <option value="STATEMENT_OF_ACCOUNT">Statements of Account (SOA)</option>
              <option value="CREDIT_NOTE">Credit Notes</option>
            </select>
          </div>

          {/* Posting Verdict Filter */}
          <select
            value={selectedVerdict}
            onChange={(e) => setSelectedVerdict(e.target.value)}
            className="border border-slate-300 rounded-lg px-2.5 py-1.5 text-xs text-slate-700 bg-white focus:outline-none focus:border-emerald-500"
          >
            <option value="ALL">All Posting Verdicts</option>
            <option value="APPROVED">Ready to Post (IRAS Verified)</option>
            <option value="REQUIRES_ATTENTION">Requires Attention</option>
            <option value="CRITICAL_BLOCKER">Posting Blocked</option>
          </select>
        </div>
      </div>

      {/* Main Documents Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 border-b border-slate-200 text-slate-600 font-semibold uppercase tracking-wider text-[10px]">
              <tr>
                <th className="py-3 px-4 min-w-[130px]">Document / Number</th>
                <th className="py-3 px-4 min-w-[190px]">Vendor &amp; Singapore UEN</th>
                <th className="py-3 px-4 min-w-[120px]">Dates</th>
                <th className="py-3 px-4 text-right min-w-[130px]">Amounts (SGD)</th>
                <th className="py-3 px-4 min-w-[125px]">3-Way Match</th>
                <th className="py-3 px-4 min-w-[135px]">AI Posting Verdict</th>
                <th className="py-3 px-4 min-w-[110px]">Xero Status</th>
                <th className="py-3 px-4 text-center min-w-[90px]">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filteredDocs.length === 0 ? (
                <tr>
                  <td colSpan={8} className="py-12 text-center text-slate-400">
                    <FileText className="w-8 h-8 mx-auto mb-2 text-slate-300" />
                    No documents matched your filters.
                  </td>
                </tr>
              ) : (
                filteredDocs.map((doc) => (
                  <tr
                    key={doc.id}
                    className="hover:bg-slate-50/80 transition-colors group cursor-pointer"
                    onClick={() => onSelectDocument(doc)}
                  >
                    {/* Document Number & Type */}
                    <td className="py-3.5 px-4">
                      <div className="space-y-1">
                        <div className="font-bold text-slate-900 group-hover:text-emerald-700 transition">
                          {doc.documentNumber}
                        </div>
                        <div>{getDocTypeBadge(doc.documentType)}</div>
                        {doc.referenceNumbers.length > 0 && (
                          <div className="text-[10px] text-slate-400 font-mono truncate max-w-[130px]">
                            Ref: {doc.referenceNumbers.join(', ')}
                          </div>
                        )}
                      </div>
                    </td>

                    {/* Vendor Name & UEN */}
                    <td className="py-3.5 px-4 min-w-[190px]">
                      <div className="space-y-0.5">
                        <div className="font-semibold text-slate-800 line-clamp-1" title={doc.vendor.name}>
                          {doc.vendor.name}
                        </div>
                        <div className="flex items-center space-x-2 text-[11px] text-slate-500">
                          {doc.vendor.uen && (
                            <span className="font-mono bg-slate-100 px-1 rounded text-[10px]">
                              UEN: {doc.vendor.uen}
                            </span>
                          )}
                          {doc.vendor.payNowUen && (
                            <span className="text-[10px] text-emerald-600 font-semibold">
                              PayNow Ready
                            </span>
                          )}
                        </div>
                      </div>
                    </td>

                    {/* Dates */}
                    <td className="py-3.5 px-4 whitespace-nowrap text-slate-600">
                      <div>
                        <span className="text-slate-400 text-[10px]">Issue: </span>
                        {doc.issueDate}
                      </div>
                      <div className="text-[11px] font-medium text-slate-700">
                        <span className="text-slate-400 text-[10px]">Due: </span>
                        {doc.dueDate}
                      </div>
                    </td>

                    {/* Financial Amounts (SGD) */}
                    <td className="py-3.5 px-4 text-right whitespace-nowrap">
                      <div className="font-bold text-slate-900">
                        {doc.currency} ${doc.totalAmount.toLocaleString('en-SG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                      </div>
                      <div className="text-[10px] text-slate-500">
                        Sub: ${doc.subtotal.toFixed(2)} | 9% GST: ${doc.gstAmount.toFixed(2)}
                      </div>
                    </td>

                    {/* 3-Way Match */}
                    <td className="py-3.5 px-4 whitespace-nowrap">
                      {getMatchBadge(doc)}
                    </td>

                    {/* Posting Verdict */}
                    <td className="py-3.5 px-4 whitespace-nowrap">
                      {getVerdictBadge(doc.postingVerdict)}
                    </td>

                    {/* Xero Status */}
                    <td className="py-3.5 px-4 whitespace-nowrap">
                      {doc.xeroPostStatus.posted ? (
                        <span className="inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[10px] font-bold bg-blue-100 text-blue-800">
                          <CheckCircle2 className="w-3 h-3 text-blue-600" />
                          <span>Posted to Xero</span>
                        </span>
                      ) : (
                        <span className="text-[11px] text-slate-400">Unposted</span>
                      )}
                    </td>

                    {/* Actions */}
                    <td
                      className="py-3.5 px-4 text-center whitespace-nowrap"
                      onClick={(e) => e.stopPropagation()}
                    >
                      <div className="flex items-center justify-center space-x-1">
                        <button
                          onClick={() => onSelectDocument(doc)}
                          className="p-1.5 text-slate-600 hover:text-emerald-700 hover:bg-emerald-50 rounded transition"
                          title="Inspect AI Extraction"
                        >
                          <Eye className="w-4 h-4" />
                        </button>

                        <button
                          onClick={() => downloadDummyInvoicePdf(doc)}
                          className="p-1.5 text-slate-600 hover:text-emerald-700 hover:bg-emerald-50 rounded transition"
                          title="Download Singapore Dummy Invoice PDF"
                        >
                          <Download className="w-4 h-4" />
                        </button>

                        {doc.documentType === 'TAX_INVOICE' && doc.threeWayMatch?.status !== 'NOT_APPLICABLE' && (
                          <button
                            onClick={() => onOpenThreeWayMatch(doc)}
                            className="p-1.5 text-slate-600 hover:text-blue-700 hover:bg-blue-50 rounded transition"
                            title="Open 3-Way Match Studio"
                          >
                            <Layers className="w-4 h-4" />
                          </button>
                        )}

                        {!doc.xeroPostStatus.posted && doc.documentType === 'TAX_INVOICE' && (
                          <button
                            onClick={() => onPostToXero(doc)}
                            className="p-1.5 text-emerald-600 hover:text-emerald-800 hover:bg-emerald-50 rounded transition"
                            title="Post to Xero (Simulated API)"
                          >
                            <Send className="w-4 h-4" />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
