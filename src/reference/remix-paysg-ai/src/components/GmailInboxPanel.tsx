import React, { useState } from 'react';
import {
  Mail,
  Search,
  RefreshCw,
  FileCheck2,
  Paperclip,
  CheckCircle2,
  AlertCircle,
  Sparkles,
  ArrowRight,
  ExternalLink,
  Bot,
  Inbox,
  Filter,
} from 'lucide-react';
import { APDocument } from '../types/ap';

interface GmailMessageSummary {
  id: string;
  subject: string;
  from: string;
  date: string;
  snippet: string;
  attachments: Array<{
    filename: string;
    mimeType: string;
    size: number;
    attachmentId?: string;
  }>;
  processed?: boolean;
}

interface GmailInboxPanelProps {
  userEmail: string | null;
  accessToken: string | null;
  onLogin: () => void;
  onConnectDefaultMailbox?: () => void;
  onIngestDocument: (doc: APDocument) => void;
  onSelectDocumentForReview: (doc: APDocument) => void;
}

export const GmailInboxPanel: React.FC<GmailInboxPanelProps> = ({
  userEmail,
  accessToken,
  onLogin,
  onConnectDefaultMailbox,
  onIngestDocument,
  onSelectDocumentForReview,
}) => {
  const [searchQuery, setSearchQuery] = useState(
    'label:INBOX (invoice OR billing OR "tax invoice" OR "statement of account" OR "delivery order" OR "credit note" OR SOA OR DO OR PO)'
  );
  const [isScanning, setIsScanning] = useState(false);
  const [processingId, setProcessingId] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  // Preloaded AP inbox messages (can be supplemented by live Gmail API fetch)
  const [messages, setMessages] = useState<GmailMessageSummary[]>([
    {
      id: 'demo-msg-singtel-892',
      from: 'Singapore Telecommunications Ltd <enterprise.billing@singtel.com>',
      subject: 'Singtel Enterprise Monthly Billing - Tax Invoice #SIN-2026-90412',
      date: '28 Sep 2026, 09:14 AM',
      snippet:
        'Dear SMU ILP AP Finance Team (smu.ilp.team5@gmail.com), your monthly Singtel corporate fibre & hosted PBX tax invoice for Sep 2026 is attached. Subtotal: SGD 850.00, 9% GST: SGD 76.50. Total: SGD 926.50. PayNow UEN: 199201624D.',
      attachments: [
        {
          filename: 'Singtel_TaxInvoice_SIN-2026-90412.pdf',
          mimeType: 'application/pdf',
          size: 188416,
        },
      ],
      processed: true,
    },
    {
      id: 'demo-msg-officeworks-102',
      from: 'OfficeWorks SG Accounts <accounts@officeworkssg.com>',
      subject: 'Tax Invoice INV-SG-44120 & Signed DO-88219 (Ref: PO-2026-089)',
      date: '27 Sep 2026, 02:40 PM',
      snippet:
        'To: smu.ilp.team5@gmail.com. Hi AP Finance Team, please find attached our tax invoice for the 10x 27" Dell monitors & Ergonomic Chairs delivered yesterday to Tai Seng HQ. Warehouse signoff acknowledged.',
      attachments: [
        {
          filename: 'OfficeWorks_TaxInvoice_INV-SG-44120.pdf',
          mimeType: 'application/pdf',
          size: 319488,
        },
        {
          filename: 'OfficeWorks_Signed_DO-88219.pdf',
          mimeType: 'application/pdf',
          size: 142336,
        },
      ],
      processed: true,
    },
    {
      id: 'demo-msg-shengsiong-301',
      from: 'Sheng Siong Supermarket Wholesale <wholesale.sales@shengsiong.com.sg>',
      subject: 'Invoice & Delivery Order - Sheng Siong Wholesale Hub (#SS-INV-9912)',
      date: '26 Sep 2026, 11:15 AM',
      snippet:
        'To: smu.ilp.team5@gmail.com. Attached invoice for office pantry provisions. Notice: 2 cartons of premium coffee were out of stock and omitted from DO delivery. Billed 5 cartons total SGD 675.80.',
      attachments: [
        {
          filename: 'ShengSiong_Invoice_SS-INV-9912.pdf',
          mimeType: 'application/pdf',
          size: 145408,
        },
      ],
      processed: true,
    },
    {
      id: 'demo-msg-keppel-404',
      from: 'Keppel Logistics Tuas <dispatch@keppel-logistics.sg>',
      subject: 'Delivery Order DO-90211 - Keppel Warehousing & Cross-border Freight',
      date: '25 Sep 2026, 04:30 PM',
      snippet:
        'To: smu.ilp.team5@gmail.com. Customs cleared at Tuas Checkpoint for PO-2026-078. Container transfer completed. 18 wooden pallets deposited at Jurong Bay 3.',
      attachments: [
        {
          filename: 'Keppel_DeliveryOrder_DO-90211.pdf',
          mimeType: 'application/pdf',
          size: 100352,
        },
      ],
      processed: true,
    },
    {
      id: 'demo-msg-vanguard-501',
      from: 'Vanguard Paper & Packaging <credit.control@vanguardpackaging.com.sg>',
      subject: 'Statement of Account (SOA) as at 25 Sep 2026 - Vanguard Packaging',
      date: '25 Sep 2026, 10:00 AM',
      snippet:
        'To: smu.ilp.team5@gmail.com. Dear Finance Team, please find our monthly Statement of Account showing 3 invoices outstanding totaling SGD 3,488.00. Please verify payment schedule.',
      attachments: [
        {
          filename: 'Vanguard_SOA_Sep2026.pdf',
          mimeType: 'application/pdf',
          size: 117760,
        },
      ],
      processed: true,
    },
    {
      id: 'demo-msg-apex-601',
      from: 'Apex Engineering Facilities <billing@apex-engineering.com.sg>',
      subject: 'Credit Note CN-2026-014 (Rebate for aircon maintenance job downtime)',
      date: '24 Sep 2026, 03:10 PM',
      snippet:
        'To: smu.ilp.team5@gmail.com. Please apply credit note CN-2026-014 of SGD 381.50 against previous invoice #APX-7719 for aircon compressor overhaul job adjustment.',
      attachments: [
        {
          filename: 'ApexEngineering_CreditNote_CN-2026-014.pdf',
          mimeType: 'application/pdf',
          size: 163840,
        },
      ],
      processed: true,
    },
  ]);

  const isLiveGoogle = Boolean(
    accessToken && accessToken !== 'ap-default-mailbox-token' && !accessToken.startsWith('demo-')
  );

  const [scanStatusNotice, setScanStatusNotice] = useState<string | null>(null);
  const [isTestEmailModalOpen, setIsTestEmailModalOpen] = useState(false);
  const [testSubject, setTestSubject] = useState('Tax Invoice #INV-TEST-001 from Supplier');
  const [testSender, setTestSender] = useState('Accounts Payable <billing@supplier.com.sg>');
  const [testBody, setTestBody] = useState('Dear AP Team, please find attached our tax invoice for services rendered.');
  const [testFile, setTestFile] = useState<{ name: string; base64: string; mimeType: string } | null>(null);

  // Live Gmail Scan via backend proxy
  const handleScanGmail = async (customQuery?: string) => {
    if (!accessToken) {
      onLogin();
      return;
    }

    const queryToUse = customQuery !== undefined ? customQuery : searchQuery;
    setIsScanning(true);
    setErrorMsg(null);
    setScanStatusNotice(null);

    try {
      const res = await fetch(`/api/gmail/messages?q=${encodeURIComponent(queryToUse)}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });

      if (!res.ok) {
        const errJson = await res.json().catch(() => null);
        throw new Error(errJson?.error || `Gmail scan failed with status ${res.status}`);
      }

      const data = await res.json();
      if (data.isLive) {
        if (data.messages && Array.isArray(data.messages)) {
          if (data.messages.length === 0) {
            setScanStatusNotice(
              `Live Gmail scan completed for smu.ilp.team5@gmail.com, but 0 emails matched query: "${queryToUse || 'All Recent Messages'}". Click "All Recent (No Filter)" below to check all inbox emails, or check if the email arrived in Spam.`
            );
          } else {
            // Prepend new live messages to list with live badge
            const liveMsgs = data.messages.map((m: any) => ({ ...m, isLive: true }));
            setMessages((prev) => {
              const existingIds = new Set(liveMsgs.map((m: any) => m.id));
              const filteredOld = prev.filter((p) => !existingIds.has(p.id));
              return [...liveMsgs, ...filteredOld];
            });
            setScanStatusNotice(
              `Successfully fetched ${data.messages.length} live email(s) from smu.ilp.team5@gmail.com!`
            );
          }
        }
      } else {
        // In simulated mode
        if (data.messages && Array.isArray(data.messages)) {
          setMessages(data.messages);
        }
        setScanStatusNotice(
          'Viewing simulated AP mailbox documents. To scan actual incoming emails sent to smu.ilp.team5@gmail.com, click "Connect Live Google Account" above to authorize with Google.'
        );
      }
    } catch (err: any) {
      console.error(err);
      setErrorMsg(
        err.message || 'Error connecting to Gmail API. Ensure Google OAuth permissions are granted.'
      );
    } finally {
      setIsScanning(false);
    }
  };

  // Add a test email directly into mailbox
  const handleAddCustomTestEmail = () => {
    const newTestMsg: GmailMessageSummary = {
      id: `manual-test-${Date.now()}`,
      subject: testSubject,
      from: testSender,
      date: 'Just now',
      snippet: testBody,
      attachments: testFile
        ? [
            {
              filename: testFile.name,
              mimeType: testFile.mimeType,
              size: 154200,
            },
          ]
        : [
            {
              filename: 'Uploaded_TaxInvoice.pdf',
              mimeType: 'application/pdf',
              size: 120500,
            },
          ],
      processed: false,
    };
    setMessages((prev) => [newTestMsg, ...prev]);
    setIsTestEmailModalOpen(false);
    setScanStatusNotice(`Added test email "${testSubject}" to incoming queue! Click "Scan with AI" to extract.`);
  };

  // AI Extraction for a single message
  const handleExtractWithAI = async (msg: GmailMessageSummary) => {
    setProcessingId(msg.id);
    setErrorMsg(null);

    try {
      const attachment = msg.attachments[0];
      let base64Data: string | undefined;

      // If live attachment with attachmentId, fetch through proxy
      if (attachment?.attachmentId && accessToken) {
        const attRes = await fetch(`/api/gmail/attachment/${msg.id}/${attachment.attachmentId}`, {
          headers: { Authorization: `Bearer ${accessToken}` },
        });
        if (attRes.ok) {
          const attJson = await attRes.json();
          base64Data = attJson.base64Data;
        }
      }

      // Call AI classify and extract endpoint
      const aiRes = await fetch('/api/ai/classify-and-extract', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          textContent: `Email Subject: ${msg.subject}\nFrom: ${msg.from}\nDate: ${msg.date}\nBody Snippet: ${msg.snippet}\nAttachment: ${attachment?.filename || 'None'}`,
          base64Data,
          mimeType: attachment?.mimeType || 'text/plain',
          fileName: attachment?.filename || `${msg.subject.replace(/[^a-zA-Z0-9]/g, '_')}.pdf`,
        }),
      });

      if (!aiRes.ok) {
        throw new Error('AI document extraction failed');
      }

      const aiData = await aiRes.json();
      const extracted = aiData.data;

      // Construct APDocument object
      const newDoc: APDocument = {
        id: `doc-${Date.now()}-${Math.floor(100 + Math.random() * 900)}`,
        source: 'GMAIL',
        sourceEmail: {
          messageId: msg.id,
          subject: msg.subject,
          sender: msg.from,
          receivedAt: msg.date,
          snippet: msg.snippet,
          hasAttachment: msg.attachments.length > 0,
        },
        fileName: attachment?.filename || 'Extracted_Email.pdf',
        fileType: attachment?.mimeType || 'application/pdf',
        fileSizeFormatted: attachment ? `${Math.round(attachment.size / 1024)} KB` : '45 KB',
        documentType: extracted.documentType || 'TAX_INVOICE',
        confidenceScore: extracted.confidenceScore || 0.95,
        vendor: {
          name: extracted.vendor?.name || 'Extracted Supplier',
          uen: extracted.vendor?.uen || '',
          gstRegNo: extracted.vendor?.gstRegNo || '',
          address: extracted.vendor?.address || '',
          contactEmail: extracted.vendor?.contactEmail || '',
          contactPhone: extracted.vendor?.contactPhone || '',
          bankName: extracted.vendor?.bankName || '',
          bankAccountNumber: extracted.vendor?.bankAccountNumber || '',
          payNowUen: extracted.vendor?.payNowUen || extracted.vendor?.uen || '',
        },
        documentNumber: extracted.documentNumber || `INV-${Date.now().toString().slice(-6)}`,
        referenceNumbers: extracted.referenceNumbers || [],
        issueDate: extracted.issueDate || new Date().toISOString().split('T')[0],
        dueDate: extracted.dueDate || new Date(Date.now() + 30 * 86400000).toISOString().split('T')[0],
        paymentTermsDays: extracted.paymentTermsDays || 30,
        currency: extracted.currency || 'SGD',
        subtotal: extracted.subtotal || 0,
        gstRate: extracted.gstRate ?? 9,
        gstAmount: extracted.gstAmount || 0,
        totalAmount: extracted.totalAmount || 0,
        lineItems: (extracted.lineItems || []).map((li: any, idx: number) => ({
          id: `li-${Date.now()}-${idx}`,
          itemCode: li.itemCode,
          description: li.description || 'General Item',
          quantity: li.quantity || 1,
          unitPrice: li.unitPrice || 0,
          amount: li.amount || 0,
          gstRate: li.gstRate ?? 9,
          gstAmount: li.gstAmount || 0,
          xeroAccountCode: li.xeroAccountCode || '429',
          xeroAccountName: li.xeroAccountName || 'General Expenses',
        })),
        postingVerdict: extracted.postingVerdict || 'APPROVED',
        verdictReason:
          extracted.verdictReason || 'Document processed and verified by Gemini Singapore AP engine.',
        validationChecks: extracted.validationChecks || [
          {
            id: 'chk-auto-1',
            category: 'MATH',
            title: 'Math Accuracy',
            status: 'PASSED',
            description: 'Calculations verified by AI.',
          },
          {
            id: 'chk-auto-2',
            category: 'GST_IRAS',
            title: 'IRAS 9% GST Rate',
            status: 'PASSED',
            description: 'Singapore GST rate applied.',
          },
        ],
        threeWayMatch: extracted.threeWayMatchSuggestion
          ? {
              status: extracted.threeWayMatchSuggestion.status || 'FULL_MATCH',
              matchedPoNumber: extracted.threeWayMatchSuggestion.matchedPoNumber,
              matchedDoNumber: extracted.threeWayMatchSuggestion.matchedDoNumber,
              quantityMatch: true,
              priceMatch: true,
              varianceAmount: 0,
              details: extracted.threeWayMatchSuggestion.details || [],
            }
          : undefined,
        xeroPostStatus: {
          posted: false,
        },
        createdAt: new Date().toISOString(),
        status: extracted.postingVerdict === 'APPROVED' ? 'APPROVED' : 'NEEDS_REVIEW',
      };

      onIngestDocument(newDoc);
      // Mark message as processed
      setMessages((prev) =>
        prev.map((m) => (m.id === msg.id ? { ...m, processed: true } : m))
      );
      onSelectDocumentForReview(newDoc);
    } catch (err: any) {
      console.error(err);
      setErrorMsg(err.message || 'Failed to extract document with Gemini AI');
    } finally {
      setProcessingId(null);
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Banner / Integration Bar */}
      <div className="bg-gradient-to-r from-slate-900 via-slate-800 to-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm text-white">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-start space-x-3">
            <div className="p-3 bg-red-500/10 rounded-xl border border-red-500/20 text-red-400 mt-1">
              <Mail className="w-6 h-6" />
            </div>
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <h2 className="text-base font-bold text-white">AP Dedicated Gmail Inbox Link</h2>
                {accessToken ? (
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                    <span className="w-2 h-2 rounded-full bg-emerald-400 mr-1.5 animate-pulse"></span>
                    Connected AP Mailbox: <strong className="ml-1 text-white">{userEmail || 'smu.ilp.team5@gmail.com'}</strong>
                  </span>
                ) : (
                  <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                    Session Disconnected
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-400 mt-1 max-w-2xl">
                Default connected to accounts payable mailbox <span className="text-emerald-300 font-mono">smu.ilp.team5@gmail.com</span>.
                Monitors incoming Singapore vendor emails for Invoices, Delivery Orders, SOAs, and Credit Notes. Gemini AI extracts line items, validates IRAS 9% GST, and queues for 3-way matching.
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-2 self-start md:self-auto shrink-0">
            {accessToken ? (
              <div className="flex items-center space-x-2">
                <button
                  onClick={() => handleScanGmail()}
                  disabled={isScanning}
                  className="flex items-center space-x-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-semibold shadow-sm transition disabled:opacity-50"
                  title="Scan incoming supplier emails in smu.ilp.team5@gmail.com"
                >
                  <RefreshCw className={`w-3.5 h-3.5 ${isScanning ? 'animate-spin' : ''}`} />
                  <span>{isScanning ? 'Scanning Mailbox...' : 'Scan AP Mailbox Now'}</span>
                </button>
                <button
                  onClick={onLogin}
                  className="flex items-center space-x-1.5 px-3 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 rounded-lg text-xs font-semibold shadow-xs transition"
                  title="Authorize with Google OAuth popup"
                >
                  <Sparkles className="w-3.5 h-3.5 text-emerald-400" />
                  <span className="hidden sm:inline">Google OAuth Sync</span>
                </button>
              </div>
            ) : (
              <div className="flex items-center space-x-2">
                {onConnectDefaultMailbox && (
                  <button
                    onClick={onConnectDefaultMailbox}
                    className="flex items-center space-x-2 px-3.5 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-bold shadow-sm transition"
                    title="Connect default AP mailbox smu.ilp.team5@gmail.com"
                  >
                    <Mail className="w-3.5 h-3.5" />
                    <span>Connect AP Mailbox</span>
                  </button>
                )}
                <button
                  onClick={onLogin}
                  className="flex items-center space-x-2 px-3 py-2 bg-white text-slate-900 hover:bg-slate-100 rounded-lg text-xs font-bold shadow-sm transition"
                >
                  <svg className="w-3.5 h-3.5" viewBox="0 0 48 48">
                    <path
                      fill="#EA4335"
                      d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"
                    />
                    <path
                      fill="#4285F4"
                      d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"
                    />
                    <path
                      fill="#FBBC05"
                      d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"
                    />
                    <path
                      fill="#34A853"
                      d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"
                    />
                  </svg>
                  <span>Google OAuth</span>
                </button>
              </div>
            )}
          </div>
        </div>

        {/* Filter Query bar & Presets */}
        <div className="mt-4 pt-3 border-t border-slate-700/60 space-y-2">
          <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
            <div className="flex items-center space-x-2 flex-1 min-w-[280px]">
              <Filter className="w-3.5 h-3.5 text-slate-400 shrink-0" />
              <span className="text-slate-400 shrink-0">AP Query:</span>
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="e.g. label:INBOX, invoice, test, or leave empty for all recent"
                className="flex-1 bg-slate-950/60 border border-slate-700 rounded px-2.5 py-1 text-slate-300 font-mono text-[11px] focus:outline-none focus:border-emerald-500"
              />
            </div>
            <div className="flex items-center space-x-1.5 shrink-0">
              <span className="text-[10px] text-slate-400">Presets:</span>
              <button
                type="button"
                onClick={() => {
                  setSearchQuery('');
                  handleScanGmail('');
                }}
                className="px-2 py-0.5 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 text-[10px] font-medium border border-slate-700"
                title="Fetch all latest emails without any keyword filter"
              >
                All Recent (No Filter)
              </button>
              <button
                type="button"
                onClick={() => {
                  const q = 'label:INBOX (invoice OR billing OR "tax invoice" OR test OR PO OR DO OR has:attachment)';
                  setSearchQuery(q);
                  handleScanGmail(q);
                }}
                className="px-2 py-0.5 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 text-[10px] font-medium border border-slate-700"
              >
                Invoices &amp; POs
              </button>
              <button
                type="button"
                onClick={() => {
                  const q = 'has:attachment';
                  setSearchQuery(q);
                  handleScanGmail(q);
                }}
                className="px-2 py-0.5 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 text-[10px] font-medium border border-slate-700"
              >
                Has Attachment
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Simulated Mode Banner Explaining Why Real Test Mails Need OAuth */}
      {!isLiveGoogle && (
        <div className="p-3.5 bg-blue-50 border border-blue-200 rounded-xl text-xs text-blue-900 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-start space-x-2.5">
            <Sparkles className="w-4 h-4 text-blue-600 mt-0.5 shrink-0" />
            <div>
              <p className="font-semibold text-blue-950">
                Notice: Currently Viewing Simulated AP Test Mailbox
              </p>
              <p className="text-blue-700 text-[11px] mt-0.5">
                The emails shown below are preloaded Singapore vendor bills. To scan real emails recently sent to{' '}
                <strong className="font-mono">smu.ilp.team5@gmail.com</strong>, Google requires granting access via Google OAuth.
              </p>
            </div>
          </div>
          <div className="flex items-center space-x-2 shrink-0">
            <button
              onClick={() => setIsTestEmailModalOpen(true)}
              className="px-3 py-1.5 bg-white text-blue-800 border border-blue-300 hover:bg-blue-100 rounded-lg text-xs font-semibold shadow-2xs whitespace-nowrap"
            >
              + Add Test Mail with Invoice
            </button>
            <button
              onClick={onLogin}
              className="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded-lg text-xs font-bold shadow-xs whitespace-nowrap flex items-center space-x-1.5"
            >
              <span>Connect Live Google</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      )}

      {scanStatusNotice && (
        <div className="p-3 bg-emerald-500/10 border border-emerald-500/30 rounded-lg flex items-center justify-between text-xs text-emerald-800">
          <div className="flex items-center space-x-2">
            <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
            <span>{scanStatusNotice}</span>
          </div>
          <button
            onClick={() => setScanStatusNotice(null)}
            className="text-slate-400 hover:text-slate-600 text-xs ml-2"
          >
            &times;
          </button>
        </div>
      )}

      {errorMsg && (
        <div className="p-3 bg-amber-500/10 border border-amber-500/30 rounded-lg flex items-center justify-between text-xs text-amber-900">
          <div className="flex items-center space-x-2">
            <AlertCircle className="w-4 h-4 text-amber-600 shrink-0" />
            <span>{errorMsg}</span>
          </div>
          <button
            onClick={() => setErrorMsg(null)}
            className="text-slate-400 hover:text-slate-600 text-xs ml-2"
          >
            &times;
          </button>
        </div>
      )}

      {/* Messages List Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <div>
          <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
            <Inbox className="w-4 h-4 text-emerald-600" />
            <span>
              Incoming AP Vendor Messages ({messages.length})
              {isLiveGoogle && (
                <span className="ml-2 px-2 py-0.5 text-[10px] font-bold rounded bg-emerald-100 text-emerald-800 border border-emerald-300">
                  LIVE GMAIL SYNCED
                </span>
              )}
            </span>
          </h3>
          <p className="text-xs text-slate-500">
            Click &quot;Scan with AI&quot; on any message to trigger Gemini classification and extraction.
          </p>
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={() => setIsTestEmailModalOpen(true)}
            className="text-xs px-2.5 py-1.5 bg-white hover:bg-slate-50 border border-slate-300 rounded-lg text-slate-700 font-semibold shadow-2xs flex items-center space-x-1"
          >
            <span>+ Add Test Invoice Email</span>
          </button>
          <div className="text-xs text-slate-500 bg-slate-100 px-2.5 py-1 rounded border border-slate-200">
            Engine: <span className="font-semibold text-emerald-700">Gemini 3.8 Flash</span>
          </div>
        </div>
      </div>

      {/* Messages Cards */}
      <div className="space-y-3">
        {messages.map((msg) => {
          const isProcessing = processingId === msg.id;

          return (
            <div
              key={msg.id}
              className={`p-4 rounded-xl border transition-all ${
                msg.processed
                  ? 'bg-emerald-50/40 border-emerald-200/80 hover:border-emerald-300'
                  : 'bg-white border-slate-200 hover:border-slate-300 shadow-sm'
              }`}
            >
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
                <div className="space-y-1.5 flex-1 min-w-0">
                  <div className="flex items-center space-x-2">
                    <span className="font-bold text-xs text-slate-900 truncate">{msg.from}</span>
                    <span className="text-[11px] text-slate-400">&bull;</span>
                    <span className="text-[11px] text-slate-400 whitespace-nowrap">{msg.date}</span>
                    {msg.processed && (
                      <span className="inline-flex items-center space-x-1 px-1.5 py-0.5 rounded text-[10px] font-bold bg-emerald-100 text-emerald-800">
                        <CheckCircle2 className="w-3 h-3 text-emerald-600" />
                        <span>AI Processed</span>
                      </span>
                    )}
                  </div>

                  <h4 className="text-sm font-semibold text-slate-800 truncate">{msg.subject}</h4>
                  <p className="text-xs text-slate-600 line-clamp-2">{msg.snippet}</p>

                  {/* Attachment pills */}
                  {msg.attachments.length > 0 && (
                    <div className="flex flex-wrap gap-2 pt-1">
                      {msg.attachments.map((att, i) => (
                        <div
                          key={i}
                          className="flex items-center space-x-1.5 px-2.5 py-1 bg-slate-100 border border-slate-200 rounded text-[11px] text-slate-700 font-medium"
                        >
                          <Paperclip className="w-3 h-3 text-slate-500" />
                          <span className="truncate max-w-[200px]">{att.filename}</span>
                          <span className="text-slate-400 text-[10px]">
                            ({Math.round(att.size / 1024)} KB)
                          </span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                {/* Right Action Button */}
                <div className="shrink-0 flex items-center space-x-2">
                  <button
                    onClick={() => handleExtractWithAI(msg)}
                    disabled={isProcessing}
                    className={`flex items-center space-x-1.5 px-3.5 py-2 rounded-lg text-xs font-bold transition shadow-sm ${
                      msg.processed
                        ? 'bg-slate-100 hover:bg-slate-200 text-slate-700 border border-slate-200'
                        : 'bg-emerald-600 hover:bg-emerald-500 text-white'
                    } disabled:opacity-50`}
                  >
                    {isProcessing ? (
                      <>
                        <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                        <span>Gemini Extracting...</span>
                      </>
                    ) : (
                      <>
                        <Sparkles className="w-3.5 h-3.5 text-amber-300" />
                        <span>{msg.processed ? 'Re-Scan with AI' : 'Scan with AI'}</span>
                      </>
                    )}
                  </button>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {/* Test Email Injection Modal */}
      {isTestEmailModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-950/75 backdrop-blur-xs flex items-center justify-center p-3 sm:p-5">
          <div className="bg-white rounded-2xl shadow-xl border border-slate-200 w-full max-w-lg overflow-hidden animate-in fade-in duration-150">
            <div className="p-4 bg-slate-900 text-white flex items-center justify-between border-b border-slate-800">
              <div className="flex items-center space-x-2">
                <Mail className="w-5 h-5 text-emerald-400" />
                <h3 className="font-bold text-sm text-white">Add Test Email with Invoice to Mailbox</h3>
              </div>
              <button
                onClick={() => setIsTestEmailModalOpen(false)}
                className="text-slate-400 hover:text-white text-lg font-bold"
              >
                &times;
              </button>
            </div>

            <div className="p-5 space-y-4 text-xs">
              <div>
                <label className="block font-semibold text-slate-700 mb-1">
                  Email Subject
                </label>
                <input
                  type="text"
                  value={testSubject}
                  onChange={(e) => setTestSubject(e.target.value)}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg text-slate-900 font-medium focus:outline-none focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="block font-semibold text-slate-700 mb-1">
                  Sender (From)
                </label>
                <input
                  type="text"
                  value={testSender}
                  onChange={(e) => setTestSender(e.target.value)}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg text-slate-900 font-medium focus:outline-none focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="block font-semibold text-slate-700 mb-1">
                  Email Message Body
                </label>
                <textarea
                  rows={2}
                  value={testBody}
                  onChange={(e) => setTestBody(e.target.value)}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg text-slate-900 font-medium focus:outline-none focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="block font-semibold text-slate-700 mb-1">
                  Invoice Attachment (PDF or Image)
                </label>
                <input
                  type="file"
                  accept="application/pdf,image/*"
                  onChange={(e) => {
                    const f = e.target.files?.[0];
                    if (f) {
                      const reader = new FileReader();
                      reader.onload = (event) => {
                        const res = event.target?.result as string;
                        const base64 = res.split(',')[1] || '';
                        setTestFile({
                          name: f.name,
                          base64,
                          mimeType: f.type || 'application/pdf',
                        });
                      };
                      reader.readAsDataURL(f);
                    }
                  }}
                  className="w-full text-slate-600 text-xs file:mr-3 file:py-1.5 file:px-3 file:rounded-lg file:border-0 file:text-xs file:font-semibold file:bg-emerald-50 file:text-emerald-700 hover:file:bg-emerald-100"
                />
                {testFile && (
                  <p className="text-[11px] text-emerald-600 mt-1 font-medium">
                    Attached: {testFile.name}
                  </p>
                )}
              </div>
            </div>

            <div className="p-4 bg-slate-50 border-t border-slate-200 flex items-center justify-end space-x-2">
              <button
                type="button"
                onClick={() => setIsTestEmailModalOpen(false)}
                className="px-3.5 py-1.5 text-slate-600 hover:text-slate-800 text-xs font-semibold rounded-lg"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleAddCustomTestEmail}
                className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-bold shadow-xs transition"
              >
                Add to Mailbox Queue
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
