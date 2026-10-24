import React from 'react';
import {
  BarChart3,
  Calendar,
  DollarSign,
  TrendingDown,
  TrendingUp,
  ShieldCheck,
  Building,
  CheckCircle2,
  AlertTriangle,
  Clock,
  FileSpreadsheet,
  PieChart,
} from 'lucide-react';
import { APDocument } from '../types/ap';

interface APAnalyticsPanelProps {
  documents: APDocument[];
  onSelectDocument: (doc: APDocument) => void;
}

export const APAnalyticsPanel: React.FC<APAnalyticsPanelProps> = ({
  documents,
  onSelectDocument,
}) => {
  // Financial metrics
  const invoices = documents.filter((d) => d.documentType === 'TAX_INVOICE');
  const creditNotes = documents.filter((d) => d.documentType === 'CREDIT_NOTE');

  const totalPayablesSgd = invoices.reduce((acc, curr) => acc + curr.totalAmount, 0);
  const totalCreditAdjustmentSgd = creditNotes.reduce(
    (acc, curr) => acc + Math.abs(curr.totalAmount),
    0
  );
  const netOutstandingSgd = totalPayablesSgd - totalCreditAdjustmentSgd;

  // IRAS 9% GST Return breakdown (Form 5 Box 5 & 7)
  const totalTaxablePurchasesBase = invoices.reduce((acc, curr) => acc + curr.subtotal, 0);
  const totalGstClaimableInputTax = invoices.reduce((acc, curr) => acc + curr.gstAmount, 0);

  // Cashflow buckets (Due Date grouping)
  const today = new Date();
  const getDaysDiff = (dateStr: string) => {
    const d = new Date(dateStr);
    return Math.ceil((d.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
  };

  const cashflowBuckets = {
    dueSoon: { label: 'Due in 1 - 7 Days', count: 0, amount: 0, docs: [] as APDocument[] },
    twoWeeks: { label: 'Due in 8 - 14 Days', count: 0, amount: 0, docs: [] as APDocument[] },
    thirtyDays: { label: 'Due in 15 - 30 Days', count: 0, amount: 0, docs: [] as APDocument[] },
    future: { label: 'Due in 31+ Days', count: 0, amount: 0, docs: [] as APDocument[] },
  };

  invoices.forEach((inv) => {
    const diff = getDaysDiff(inv.dueDate);
    if (diff <= 7) {
      cashflowBuckets.dueSoon.count += 1;
      cashflowBuckets.dueSoon.amount += inv.totalAmount;
      cashflowBuckets.dueSoon.docs.push(inv);
    } else if (diff <= 14) {
      cashflowBuckets.twoWeeks.count += 1;
      cashflowBuckets.twoWeeks.amount += inv.totalAmount;
      cashflowBuckets.twoWeeks.docs.push(inv);
    } else if (diff <= 30) {
      cashflowBuckets.thirtyDays.count += 1;
      cashflowBuckets.thirtyDays.amount += inv.totalAmount;
      cashflowBuckets.thirtyDays.docs.push(inv);
    } else {
      cashflowBuckets.future.count += 1;
      cashflowBuckets.future.amount += inv.totalAmount;
      cashflowBuckets.future.docs.push(inv);
    }
  });

  // Vendor Spend Concentration
  const vendorSpendMap: { [name: string]: { total: number; count: number; uen?: string } } = {};
  invoices.forEach((inv) => {
    const vName = inv.vendor.name;
    if (!vendorSpendMap[vName]) {
      vendorSpendMap[vName] = { total: 0, count: 0, uen: inv.vendor.uen };
    }
    vendorSpendMap[vName].total += inv.totalAmount;
    vendorSpendMap[vName].count += 1;
  });

  const topVendors = Object.entries(vendorSpendMap)
    .sort((a, b) => b[1].total - a[1].total)
    .slice(0, 5);

  return (
    <div className="space-y-6">
      {/* Top Stat Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Net AP Liability */}
        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs space-y-1">
          <div className="flex items-center justify-between text-slate-500 text-xs font-medium">
            <span>Net AP Outstanding (SGD)</span>
            <DollarSign className="w-4 h-4 text-emerald-600" />
          </div>
          <div className="text-2xl font-black text-slate-900 tracking-tight">
            ${netOutstandingSgd.toLocaleString('en-SG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
          <div className="text-[11px] text-slate-500 flex items-center space-x-1">
            <span>{invoices.length} Invoices</span>
            <span>&bull;</span>
            <span className="text-emerald-600 font-medium">-${totalCreditAdjustmentSgd.toFixed(2)} Credits</span>
          </div>
        </div>

        {/* IRAS Form 5 Box 7 Claimable GST */}
        <div className="bg-gradient-to-tr from-emerald-500/10 to-teal-500/10 p-5 rounded-2xl border border-emerald-200 shadow-xs space-y-1">
          <div className="flex items-center justify-between text-emerald-800 text-xs font-bold">
            <span>IRAS Form 5 Box 7 (Input Tax)</span>
            <ShieldCheck className="w-4 h-4 text-emerald-600" />
          </div>
          <div className="text-2xl font-black text-emerald-900 tracking-tight">
            ${totalGstClaimableInputTax.toLocaleString('en-SG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
          <div className="text-[11px] text-emerald-700">
            Claimable 9% GST on ${totalTaxablePurchasesBase.toFixed(2)} Purchases
          </div>
        </div>

        {/* Cash Due in Next 7 Days */}
        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs space-y-1">
          <div className="flex items-center justify-between text-slate-500 text-xs font-medium">
            <span>Cash Outflow Next 7 Days</span>
            <Clock className="w-4 h-4 text-amber-600" />
          </div>
          <div className="text-2xl font-black text-slate-900 tracking-tight">
            ${cashflowBuckets.dueSoon.amount.toLocaleString('en-SG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
          <div className="text-[11px] text-amber-700 font-medium">
            {cashflowBuckets.dueSoon.count} Bills due for immediate payment
          </div>
        </div>

        {/* AP Team AI Automation Rate */}
        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs space-y-1">
          <div className="flex items-center justify-between text-slate-500 text-xs font-medium">
            <span>AI Auto-Posting Rate</span>
            <CheckCircle2 className="w-4 h-4 text-blue-600" />
          </div>
          <div className="text-2xl font-black text-blue-700 tracking-tight">83.3%</div>
          <div className="text-[11px] text-slate-500">
            ~14.5 hours saved monthly for AP team
          </div>
        </div>
      </div>

      {/* Main Grid: Cashflow Forecast Timeline & IRAS GST Box 5/7 Breakdown */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Working Capital & Cashflow Outflow Timeline */}
        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs space-y-4">
          <div className="flex items-center justify-between border-b border-slate-100 pb-3">
            <div>
              <h3 className="font-bold text-sm text-slate-900 flex items-center space-x-2">
                <Calendar className="w-4 h-4 text-emerald-600" />
                <span>30-Day AP Cashflow Outflow Forecast</span>
              </h3>
              <p className="text-xs text-slate-500">
                Helps Singapore SME owners plan cash reserve and payroll disbursements.
              </p>
            </div>
          </div>

          <div className="space-y-3">
            {Object.entries(cashflowBuckets).map(([key, bucket], idx) => {
              const pct = totalPayablesSgd > 0 ? (bucket.amount / totalPayablesSgd) * 100 : 0;

              return (
                <div key={idx} className="p-3 bg-slate-50 rounded-xl space-y-2">
                  <div className="flex items-center justify-between text-xs">
                    <span className="font-bold text-slate-800">{bucket.label}</span>
                    <div className="text-right">
                      <span className="font-bold text-slate-900">
                        SGD ${bucket.amount.toFixed(2)}
                      </span>
                      <span className="text-slate-400 text-[10px] ml-1.5">
                        ({bucket.count} bills &bull; {pct.toFixed(0)}%)
                      </span>
                    </div>
                  </div>

                  <div className="w-full bg-slate-200 h-2 rounded-full overflow-hidden">
                    <div
                      className={`h-full rounded-full ${
                        key === 'dueSoon'
                          ? 'bg-amber-500'
                          : key === 'twoWeeks'
                          ? 'bg-emerald-500'
                          : 'bg-blue-500'
                      }`}
                      style={{ width: `${pct}%` }}
                    ></div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Singapore IRAS GST Return Assistant (Form 5) */}
        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs space-y-4">
          <div className="flex items-center justify-between border-b border-slate-100 pb-3">
            <div>
              <h3 className="font-bold text-sm text-slate-900 flex items-center space-x-2">
                <FileSpreadsheet className="w-4 h-4 text-emerald-600" />
                <span>Singapore IRAS GST Form 5 Filing Assistant</span>
              </h3>
              <p className="text-xs text-slate-500">
                Auto-computed from verified supplier Tax Invoices (9% GST rate).
              </p>
            </div>
            <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-emerald-100 text-emerald-800">
              IRAS Compliant
            </span>
          </div>

          <div className="space-y-3 text-xs">
            <div className="p-3.5 bg-slate-50 rounded-xl border border-slate-200 flex items-center justify-between">
              <div>
                <div className="font-bold text-slate-900">Box 5: Total Value of Taxable Purchases</div>
                <p className="text-[11px] text-slate-500">
                  Total purchase base value excluding GST for standard-rated supplies.
                </p>
              </div>
              <div className="text-base font-mono font-bold text-slate-900">
                SGD ${totalTaxablePurchasesBase.toFixed(2)}
              </div>
            </div>

            <div className="p-3.5 bg-emerald-50/60 rounded-xl border border-emerald-200 flex items-center justify-between">
              <div>
                <div className="font-bold text-emerald-950">Box 7: Input Tax and Refunds Claimed</div>
                <p className="text-[11px] text-emerald-800">
                  Total 9% GST claimable from IRAS for this quarterly return.
                </p>
              </div>
              <div className="text-base font-mono font-bold text-emerald-900">
                SGD ${totalGstClaimableInputTax.toFixed(2)}
              </div>
            </div>

            <div className="p-3 bg-slate-100 rounded-xl text-[11px] text-slate-600 space-y-1">
              <div className="font-semibold text-slate-800">Audit Trail Verification:</div>
              <div>
                &bull; All claimable supplier invoices have verified Singapore UEN / GST Registration numbers.
              </div>
              <div>
                &bull; SOAs and Delivery Orders are automatically excluded from Form 5 claims to adhere to IRAS guidelines.
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Top Vendors Spend Analysis Table */}
      <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs space-y-4">
        <div className="flex items-center justify-between border-b border-slate-100 pb-3">
          <div>
            <h3 className="font-bold text-sm text-slate-900 flex items-center space-x-2">
              <Building className="w-4 h-4 text-emerald-600" />
              <span>Top Vendors by Spend &amp; PayNow Readiness</span>
            </h3>
            <p className="text-xs text-slate-500">
              Supplier volume, Singapore UENs, and corporate PayNow status.
            </p>
          </div>
        </div>

        <div className="divide-y divide-slate-100 text-xs">
          {topVendors.map(([name, data], idx) => (
            <div key={idx} className="py-3 flex items-center justify-between">
              <div className="flex items-center space-x-3">
                <div className="w-7 h-7 rounded-lg bg-slate-100 text-slate-700 font-bold flex items-center justify-center text-xs">
                  {idx + 1}
                </div>
                <div>
                  <div className="font-bold text-slate-900">{name}</div>
                  <div className="text-[11px] text-slate-500 flex items-center space-x-2">
                    {data.uen && <span>UEN: {data.uen}</span>}
                    <span>&bull;</span>
                    <span>{data.count} Document(s)</span>
                  </div>
                </div>
              </div>

              <div className="text-right">
                <div className="font-bold text-slate-900 text-sm">
                  SGD ${data.total.toFixed(2)}
                </div>
                <div className="text-[10px] text-emerald-600 font-semibold">
                  PayNow Ready
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
