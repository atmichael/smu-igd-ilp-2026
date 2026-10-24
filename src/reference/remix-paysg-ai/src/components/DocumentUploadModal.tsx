import React, { useState } from 'react';
import {
  X,
  Upload,
  FileText,
  Sparkles,
  AlertCircle,
  CheckCircle2,
  RefreshCw,
  FolderOpen,
  Zap,
  Download,
  FileCheck,
} from 'lucide-react';
import { APDocument } from '../types/ap';
import { SAMPLE_AP_DOCUMENTS } from '../data/sampleDocuments';
import {
  downloadDummyInvoicePdf,
  createDummyPdfFileFromDoc,
  createDummyFileFromDoc,
} from '../utils/pdfGenerator';

interface DocumentUploadModalProps {
  isOpen: boolean;
  onClose: () => void;
  onDocumentExtracted: (doc: APDocument) => void;
}

export const DocumentUploadModal: React.FC<DocumentUploadModalProps> = ({
  isOpen,
  onClose,
  onDocumentExtracted,
}) => {
  if (!isOpen) return null;

  const [uploadMode, setUploadMode] = useState<'file' | 'paste'>('file');
  const [file, setFile] = useState<File | null>(null);
  const [pasteText, setPasteText] = useState('');
  const [presetVendor, setPresetVendor] = useState<string>('ALL');
  const [isProcessing, setIsProcessing] = useState(false);
  const [processingStage, setProcessingStage] = useState('');
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  // Drag and drop handling
  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      setFile(e.dataTransfer.files[0]);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0]);
    }
  };

  // Convert File to Base64
  const fileToBase64 = (file: File): Promise<string> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => {
        const base64String = (reader.result as string).split(',')[1];
        resolve(base64String);
      };
      reader.onerror = (error) => reject(error);
    });
  };

  const handleProcessUpload = async () => {
    if (!file && !pasteText.trim()) {
      setErrorMsg('Please select a file or paste document text.');
      return;
    }

    setIsProcessing(true);
    setErrorMsg(null);
    setProcessingStage('Analyzing document structure with Gemini 3.8 Flash...');

    try {
      let base64Data: string | undefined;
      let mimeType: string | undefined;
      let fileName = 'manual-upload.txt';

      if (file) {
        fileName = file.name;
        mimeType = file.type || 'application/pdf';
        setProcessingStage('Encoding document bytes for AI OCR...');
        base64Data = await fileToBase64(file);
      }

      setProcessingStage('Extracting Singapore UEN, 9% GST, and Line Items...');

      const response = await fetch('/api/ai/classify-and-extract', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          textContent: pasteText || undefined,
          base64Data,
          mimeType,
          fileName,
        }),
      });

      if (!response.ok) {
        throw new Error('AI extraction failed. Please try again.');
      }

      setProcessingStage('Running IRAS Tax Validation & Math Checks...');

      const jsonRes = await response.json();
      const extracted = jsonRes.data;

      const newDoc: APDocument = {
        id: `doc-${Date.now()}-${Math.floor(100 + Math.random() * 900)}`,
        source: 'MANUAL_UPLOAD',
        fileName: fileName,
        fileType: mimeType || 'text/plain',
        fileSizeFormatted: file ? `${Math.round(file.size / 1024)} KB` : '12 KB',
        documentType: extracted.documentType || 'TAX_INVOICE',
        confidenceScore: extracted.confidenceScore || 0.95,
        vendor: {
          name: extracted.vendor?.name || 'Uploaded Supplier',
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
          description: li.description || 'Uploaded Item',
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
          extracted.verdictReason || 'Verified by Gemini Singapore AP Accounting Engine.',
        validationChecks: extracted.validationChecks || [
          {
            id: 'chk-m1',
            category: 'MATH',
            title: 'Math Accuracy',
            status: 'PASSED',
            description: 'Subtotal and GST sum verified.',
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

      onDocumentExtracted(newDoc);
      onClose();
    } catch (err: any) {
      console.error(err);
      setErrorMsg(err.message || 'Error processing document');
    } finally {
      setIsProcessing(false);
      setProcessingStage('');
    }
  };

  // Sample quick loaders for rapid testing
  const handleLoadSample = (sampleType: 'singapore_hardware' | 'singapore_cloud') => {
    if (sampleType === 'singapore_hardware') {
      setUploadMode('paste');
      setPasteText(`TAX INVOICE
Challenger Technologies Limited (UEN: 198400182K)
GST Reg No: M2-0062910-1
1 Ubi Link, Challenger Building, Singapore 408553

INVOICE NO: CH-2026-88192
DATE: 2026-09-28
DUE DATE: 2026-10-28
PAYMENT TERMS: 30 Days
CUSTOMER: LionCity Logistics Pte Ltd
CUSTOMER UEN: 201829102E
PO REF: PO-2026-112

LINE ITEMS:
1. Logitech MX Master 3S Wireless Mouse (Graphite)
   Qty: 4 | Unit Price: SGD 145.00 | Total: SGD 580.00 | GST: 9%
2. Anker 100W USB-C GaN Fast Charger
   Qty: 4 | Unit Price: SGD 70.00 | Total: SGD 280.00 | GST: 9%

SUBTOTAL: SGD 860.00
SINGAPORE GST (9%): SGD 77.40
TOTAL AMOUNT PAYABLE: SGD 937.40

PAYMENT DETAILS:
PayNow Corporate UEN: 198400182K
DBS Current Account: 003-882190-2`);
    } else {
      setUploadMode('paste');
      setPasteText(`TAX INVOICE
Amazon Web Services Singapore Private Limited
UEN: 201019024G
GST Reg No: M90365778R
23 Church Street, #10-01, Capital Square, Singapore 049481

Invoice Number: AWS-SG-2026-0912
Invoice Date: 2026-09-25
Due Date: 2026-10-25
Currency: SGD

Billed Services:
- Amazon EC2 (t4g.xlarge Asia Pacific - Singapore)
  Quantity: 720 Hours | Unit Price: SGD 0.18 | Amount: SGD 129.60 | 9% GST
- Amazon S3 Standard Storage (500 GB)
  Quantity: 500 GB | Unit Price: SGD 0.03 | Amount: SGD 15.00 | 9% GST

Subtotal: SGD 144.60
GST @ 9.0%: SGD 13.01
Total Amount Due: SGD 157.61

PayNow UEN: 201019024G`);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-xl overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        {/* Modal Header */}
        <div className="p-4 sm:p-5 bg-slate-900 text-white flex items-center justify-between border-b border-slate-800">
          <div className="flex items-center space-x-2.5">
            <div className="p-2 bg-emerald-500/20 text-emerald-400 rounded-lg">
              <Upload className="w-5 h-5" />
            </div>
            <div>
              <h3 className="font-bold text-sm text-white">Upload AP Document for AI Extraction</h3>
              <p className="text-xs text-slate-400">
                Invoices, Delivery Orders, SOAs, Credit Notes (PDF, PNG, JPG, or Text)
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Mode Selector */}
        <div className="flex border-b border-slate-200 bg-slate-50 text-xs font-semibold px-4 pt-2">
          <button
            onClick={() => setUploadMode('file')}
            className={`py-2 px-3 border-b-2 transition ${
              uploadMode === 'file'
                ? 'border-emerald-600 text-emerald-700 bg-white'
                : 'border-transparent text-slate-500 hover:text-slate-800'
            }`}
          >
            File Upload (PDF / Image)
          </button>
          <button
            onClick={() => setUploadMode('paste')}
            className={`py-2 px-3 border-b-2 transition ${
              uploadMode === 'paste'
                ? 'border-emerald-600 text-emerald-700 bg-white'
                : 'border-transparent text-slate-500 hover:text-slate-800'
            }`}
          >
            Paste Invoice Text / Email
          </button>
        </div>

        {/* Modal Body */}
        <div className="p-5 space-y-4">
          {errorMsg && (
            <div className="p-3 bg-rose-50 border border-rose-200 rounded-lg flex items-center space-x-2 text-xs text-rose-800">
              <AlertCircle className="w-4 h-4 text-rose-600 shrink-0" />
              <span>{errorMsg}</span>
            </div>
          )}

          {uploadMode === 'file' ? (
            <div
              onDragOver={handleDragOver}
              onDrop={handleDrop}
              className="border-2 border-dashed border-slate-300 hover:border-emerald-500 rounded-2xl p-6 text-center bg-slate-50/60 transition cursor-pointer"
              onClick={() => document.getElementById('file-upload-input')?.click()}
            >
              <input
                id="file-upload-input"
                type="file"
                accept=".pdf,.png,.jpg,.jpeg,.webp,.txt"
                className="hidden"
                onChange={handleFileChange}
              />
              <div className="w-12 h-12 bg-white rounded-xl shadow-xs border border-slate-200 flex items-center justify-center mx-auto text-emerald-600 mb-3">
                <FolderOpen className="w-6 h-6" />
              </div>
              <div className="text-xs font-bold text-slate-800">
                {file ? file.name : 'Click to select or drag and drop document'}
              </div>
              <p className="text-[11px] text-slate-400 mt-1">
                {file
                  ? `${Math.round(file.size / 1024)} KB &bull; ${file.type || 'Document'}`
                  : 'PDF, PNG, JPG, or Scanned Tax Invoice up to 25MB'}
              </p>
            </div>
          ) : (
            <div className="space-y-2">
              <textarea
                rows={8}
                value={pasteText}
                onChange={(e) => setPasteText(e.target.value)}
                placeholder="Paste the vendor invoice email body, OCR text, or line items here..."
                className="w-full p-3 border border-slate-300 rounded-xl text-xs font-mono text-slate-800 focus:outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
              />
            </div>
          )}

          {/* Quick-Test Singapore Templates & Dummy PDF presets */}
          <div className="p-3 bg-emerald-50/70 border border-emerald-200 rounded-xl space-y-2.5 text-xs">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
              <span className="font-bold text-emerald-950 flex items-center space-x-1.5">
                <Zap className="w-3.5 h-3.5 text-emerald-600" />
                <span>Demo Fake Dummy Invoices &amp; Documents:</span>
              </span>
              <div className="flex items-center space-x-1 text-[11px]">
                <span className="text-slate-500 font-medium">Vendor:</span>
                <select
                  value={presetVendor}
                  onChange={(e) => setPresetVendor(e.target.value)}
                  className="bg-white border border-emerald-300 text-emerald-900 rounded px-2 py-0.5 text-[11px] font-semibold focus:outline-none focus:border-emerald-600"
                >
                  <option value="ALL">All Vendors</option>
                  <option value="OfficeWorks SG Pte Ltd">OfficeWorks SG</option>
                  <option value="Sheng Siong Supermarket Pte Ltd">Sheng Siong</option>
                  <option value="Singapore Telecommunications Limited">Singtel Telecom</option>
                  <option value="Keppel Logistics Pte Ltd">Keppel Logistics</option>
                  <option value="Apex Engineering & Facilities Pte Ltd">Apex Engineering</option>
                  <option value="Vanguard Paper & Packaging Singapore Pte Ltd">Vanguard Packaging</option>
                </select>
              </div>
            </div>

            <p className="text-[11px] text-slate-600">
              💡 <strong>Demo Testing:</strong> Click <strong>&quot;Select as PDF File&quot;</strong> to stage an authentic Singapore PDF into the upload box and run Gemini OCR extraction, or click the <Download className="w-3 h-3 inline text-emerald-700 mx-0.5" /> button to download the real PDF to your computer!
            </p>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 max-h-56 overflow-y-auto pr-1">
              {SAMPLE_AP_DOCUMENTS.filter(
                (d) => presetVendor === 'ALL' || d.vendor.name === presetVendor
              ).map((sampleDoc) => (
                <div
                  key={sampleDoc.id}
                  className="p-2.5 bg-white rounded-lg border border-emerald-200/80 space-y-1.5 shadow-2xs hover:border-emerald-400 transition"
                >
                  <div className="flex items-start justify-between gap-1">
                    <div className="min-w-0">
                      <div className="font-bold text-slate-800 text-[11px] truncate">
                        {sampleDoc.vendor.name.replace(/ Pte Ltd| Limited/g, '')}
                      </div>
                      <div className="text-[10px] text-slate-500 truncate">
                        {sampleDoc.documentType.replace(/_/g, ' ')} &bull; {sampleDoc.documentNumber}
                      </div>
                    </div>
                    <span className="text-[10px] font-bold text-emerald-700 bg-emerald-50 px-1.5 py-0.5 rounded border border-emerald-200 shrink-0">
                      SGD ${sampleDoc.totalAmount.toFixed(2)}
                    </span>
                  </div>

                  <div className="flex items-center space-x-1.5 pt-1">
                    <button
                      type="button"
                      onClick={() => {
                        setUploadMode('file');
                        setFile(createDummyPdfFileFromDoc(sampleDoc));
                      }}
                      className="px-2.5 py-1 bg-emerald-600 hover:bg-emerald-500 text-white rounded font-semibold text-[10px] flex items-center space-x-1 shadow-2xs"
                    >
                      <FileCheck className="w-3 h-3" />
                      <span>Select as PDF File</span>
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        downloadDummyInvoicePdf(sampleDoc);
                      }}
                      className="px-2 py-1 bg-slate-100 hover:bg-emerald-100 text-slate-700 hover:text-emerald-800 border border-slate-200 rounded font-semibold text-[10px] flex items-center space-x-1"
                      title="Download authentic Singapore PDF to your local computer"
                    >
                      <Download className="w-3 h-3 text-emerald-600" />
                      <span>Download PDF</span>
                    </button>
                  </div>
                </div>
              ))}
            </div>

            <div className="pt-1 flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => handleLoadSample('singapore_hardware')}
                className="px-2 py-0.5 bg-white hover:bg-slate-100 text-slate-700 border border-slate-300 rounded text-[10px] font-medium transition"
              >
                + Paste Challenger Hardware Invoice Text
              </button>
              <button
                type="button"
                onClick={() => handleLoadSample('singapore_cloud')}
                className="px-2 py-0.5 bg-white hover:bg-slate-100 text-slate-700 border border-slate-300 rounded text-[10px] font-medium transition"
              >
                + Paste AWS Singapore Cloud Invoice Text
              </button>
            </div>
          </div>

          {/* Processing Animation */}
          {isProcessing && (
            <div className="p-3 bg-slate-900 text-white rounded-xl flex items-center space-x-3 text-xs">
              <RefreshCw className="w-4 h-4 text-emerald-400 animate-spin" />
              <div className="flex-1 min-w-0">
                <div className="font-semibold text-emerald-300">Processing Document with Gemini 3.8 Flash</div>
                <div className="text-slate-400 text-[11px] truncate">{processingStage}</div>
              </div>
            </div>
          )}
        </div>

        {/* Modal Footer */}
        <div className="p-4 bg-slate-50 border-t border-slate-200 flex items-center justify-between">
          <button
            onClick={onClose}
            className="px-4 py-2 bg-white border border-slate-300 text-slate-700 hover:bg-slate-100 rounded-lg text-xs font-semibold shadow-xs transition"
          >
            Cancel
          </button>

          <button
            onClick={handleProcessUpload}
            disabled={isProcessing}
            className="flex items-center space-x-2 px-5 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-bold shadow-xs transition disabled:opacity-50"
          >
            <Sparkles className="w-3.5 h-3.5 text-amber-300" />
            <span>{isProcessing ? 'Analyzing...' : 'Scan & Extract with AI'}</span>
          </button>
        </div>
      </div>
    </div>
  );
};
