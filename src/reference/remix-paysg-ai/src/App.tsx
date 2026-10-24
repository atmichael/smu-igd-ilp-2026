import React, { useState, useEffect } from 'react';
import {
  User,
  initAuth,
  googleSignIn,
  logout,
  getAccessToken,
  setCachedToken,
} from './services/auth';
import { Header } from './components/Header';
import { GmailInboxPanel } from './components/GmailInboxPanel';
import { DocumentList } from './components/DocumentList';
import { DocumentDetailModal } from './components/DocumentDetailModal';
import { DocumentUploadModal } from './components/DocumentUploadModal';
import { TechDesignDocModal } from './components/TechDesignDocModal';
import { FirebaseAuthDomainModal } from './components/FirebaseAuthDomainModal';
import { ThreeWayMatchStudio } from './components/ThreeWayMatchStudio';
import { XeroIntegrationPanel } from './components/XeroIntegrationPanel';
import { APAnalyticsPanel } from './components/APAnalyticsPanel';
import { SAMPLE_AP_DOCUMENTS } from './data/sampleDocuments';
import { APDocument, LineItem } from './types/ap';
import { CheckCircle2, AlertCircle, Info, Sparkles, Plus, Upload } from 'lucide-react';

export default function App() {
  const [activeTab, setActiveTab] = useState<
    'inbox' | 'documents' | 'matching' | 'xero' | 'analytics'
  >('documents');

  // Documents state initialized with rich Singapore sample AP documents
  const [documents, setDocuments] = useState<APDocument[]>(SAMPLE_AP_DOCUMENTS);

  // Selected document for deep inspection modal
  const [selectedDoc, setSelectedDoc] = useState<APDocument | null>(null);

  // Upload modal state
  const [isUploadOpen, setIsUploadOpen] = useState(false);

  // Tech Design Document modal state
  const [isTechDocsOpen, setIsTechDocsOpen] = useState(false);

  // Firebase Auth domain error modal
  const [isAuthDomainModalOpen, setIsAuthDomainModalOpen] = useState(false);

  // Default AP Finance Mailbox credentials (smu.ilp.team5@gmail.com)
  const DEFAULT_AP_USER: Partial<User> = {
    email: 'smu.ilp.team5@gmail.com',
    displayName: 'SMU ILP Team 5 (AP Finance Lead)',
    uid: 'ap-mailbox-smu-ilp-team5',
  };
  const DEFAULT_AP_TOKEN = 'ap-default-mailbox-token';

  // Auth & Gmail state - default connect to user's AP mailbox (ho.wei.zhe@gmail.com)
  const [user, setUser] = useState<User | null>(() => {
    const saved = localStorage.getItem('paysg_user');
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch (e) {
        // fallback
      }
    }
    const isLoggedOut = localStorage.getItem('paysg_explicitly_logged_out') === 'true';
    if (isLoggedOut) return null;
    return DEFAULT_AP_USER as User;
  });

  const [accessToken, setAccessToken] = useState<string | null>(() => {
    const saved = localStorage.getItem('paysg_token');
    if (saved) return saved;
    const isLoggedOut = localStorage.getItem('paysg_explicitly_logged_out') === 'true';
    if (isLoggedOut) return null;
    return DEFAULT_AP_TOKEN;
  });

  const [isLoggingIn, setIsLoggingIn] = useState(false);

  // Toast notification state
  const [toast, setToast] = useState<{
    type: 'success' | 'error' | 'info';
    message: string;
  } | null>(null);

  const showToast = (message: string, type: 'success' | 'error' | 'info' = 'success') => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  // Initialize Firebase Auth listener
  useEffect(() => {
    if (accessToken) {
      setCachedToken(accessToken);
    }
    const unsubscribe = initAuth(
      (authedUser, token) => {
        setUser(authedUser);
        setAccessToken(token);
        setCachedToken(token);
        localStorage.setItem(
          'paysg_user',
          JSON.stringify({
            email: authedUser.email,
            displayName: authedUser.displayName,
            photoURL: authedUser.photoURL,
            uid: authedUser.uid,
          })
        );
        localStorage.setItem('paysg_token', token);
        localStorage.removeItem('paysg_explicitly_logged_out');
      },
      () => {
        // If not authenticated via Firebase popup and not explicitly signed out, keep default AP mailbox
        const isLoggedOut = localStorage.getItem('paysg_explicitly_logged_out') === 'true';
        if (!isLoggedOut && !user) {
          setUser(DEFAULT_AP_USER as User);
          setAccessToken(DEFAULT_AP_TOKEN);
          setCachedToken(DEFAULT_AP_TOKEN);
        }
      }
    );
    return () => {
      if (typeof unsubscribe === 'function') {
        unsubscribe();
      }
    };
  }, []);

  const handleLogin = async () => {
    setIsLoggingIn(true);
    try {
      const result = await googleSignIn();
      if (result) {
        setUser(result.user);
        setAccessToken(result.accessToken);
        setCachedToken(result.accessToken);
        localStorage.setItem(
          'paysg_user',
          JSON.stringify({
            email: result.user.email,
            displayName: result.user.displayName,
            photoURL: result.user.photoURL,
            uid: result.user.uid,
          })
        );
        localStorage.setItem('paysg_token', result.accessToken);
        localStorage.removeItem('paysg_explicitly_logged_out');
        showToast(`Connected live to Gmail as ${result.user.email}! Scanning inbox...`, 'success');
        setActiveTab('inbox');
      }
    } catch (err: any) {
      console.error('Sign in failed:', err);
      if (
        err?.code === 'auth/unauthorized-domain' ||
        err?.message?.includes('auth/unauthorized-domain')
      ) {
        setIsAuthDomainModalOpen(true);
      } else {
        showToast(err.message || 'Google sign in cancelled or failed', 'error');
      }
    } finally {
      setIsLoggingIn(false);
    }
  };

  const handleConnectDefaultMailbox = () => {
    setUser(DEFAULT_AP_USER as User);
    setAccessToken(DEFAULT_AP_TOKEN);
    setCachedToken(DEFAULT_AP_TOKEN);
    localStorage.removeItem('paysg_explicitly_logged_out');
    localStorage.setItem('paysg_user', JSON.stringify(DEFAULT_AP_USER));
    localStorage.setItem('paysg_token', DEFAULT_AP_TOKEN);
    showToast('Default connected to AP mailbox (smu.ilp.team5@gmail.com)!', 'success');
    setActiveTab('inbox');
  };

  const handleEnableDemoMode = () => {
    handleConnectDefaultMailbox();
  };

  const handleLogout = async () => {
    try {
      await logout();
    } catch (e) {
      // ignore
    }
    setUser(null);
    setAccessToken(null);
    setCachedToken(null);
    localStorage.removeItem('paysg_user');
    localStorage.removeItem('paysg_token');
    localStorage.setItem('paysg_explicitly_logged_out', 'true');
    showToast('Disconnected AP mailbox session', 'info');
  };

  // Add newly extracted document (from Gmail or upload)
  const handleIngestDocument = (doc: APDocument) => {
    setDocuments((prev) => [doc, ...prev]);
    showToast(`Document #${doc.documentNumber} extracted & classified by Gemini AI!`, 'success');
  };

  // Post single document to Xero
  const handlePostToXero = async (doc: APDocument) => {
    try {
      showToast(`Syncing #${doc.documentNumber} to Xero Singapore...`, 'info');

      const res = await fetch('/api/xero/post-bill', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          documentId: doc.id,
          xeroPayload: doc.xeroPayload || {
            contactName: doc.vendor.name,
            invoiceNumber: doc.documentNumber,
            date: doc.issueDate,
            dueDate: doc.dueDate,
            currency: doc.currency,
          },
        }),
      });

      if (!res.ok) {
        throw new Error('Failed to post bill to Xero API');
      }

      const json = await res.json();
      const updatedDocs = documents.map((d) =>
        d.id === doc.id
          ? {
              ...d,
              xeroPostStatus: {
                posted: true,
                xeroBillId: json.xeroBillId,
                postedAt: json.postedAt,
                statusBadge: 'Synced to Xero ACCPAY',
              },
              status: 'POSTED_TO_XERO' as const,
            }
          : d
      );

      setDocuments(updatedDocs);
      if (selectedDoc?.id === doc.id) {
        setSelectedDoc({
          ...selectedDoc,
          xeroPostStatus: {
            posted: true,
            xeroBillId: json.xeroBillId,
            postedAt: json.postedAt,
            statusBadge: 'Synced to Xero ACCPAY',
          },
          status: 'POSTED_TO_XERO',
        });
      }

      showToast(`Bill #${doc.documentNumber} successfully posted to Xero! (${json.xeroBillId})`, 'success');
    } catch (err: any) {
      console.error(err);
      showToast(err.message || 'Error posting to Xero', 'error');
    }
  };

  // Batch post multiple documents to Xero
  const handleBatchPostToXero = async (docIds: string[]) => {
    showToast(`Batch posting ${docIds.length} bills to Xero...`, 'info');
    for (const id of docIds) {
      const doc = documents.find((d) => d.id === id);
      if (doc) {
        await handlePostToXero(doc);
      }
    }
    showToast(`Batch posting completed for ${docIds.length} bills!`, 'success');
  };

  // Approve document
  const handleApproveDocument = (docId: string) => {
    setDocuments((prev) =>
      prev.map((d) => (d.id === docId ? { ...d, status: 'APPROVED', postingVerdict: 'APPROVED' } : d))
    );
    showToast('Document marked as approved for posting!', 'success');
  };

  // Update line items
  const handleUpdateLineItems = (docId: string, updatedLineItems: LineItem[]) => {
    setDocuments((prev) =>
      prev.map((d) => (d.id === docId ? { ...d, lineItems: updatedLineItems } : d))
    );
    showToast('Line items updated successfully', 'success');
  };

  // Computed count
  const readyToPostCount = documents.filter(
    (d) => d.documentType === 'TAX_INVOICE' && !d.xeroPostStatus.posted
  ).length;

  return (
    <div className="min-h-screen bg-slate-100 flex flex-col font-sans text-slate-900">
      {/* Header */}
      <Header
        activeTab={activeTab}
        setActiveTab={setActiveTab}
        user={user}
        onLogin={handleLogin}
        onLogout={handleLogout}
        onConnectDefaultMailbox={handleConnectDefaultMailbox}
        isLoggingIn={isLoggingIn}
        onOpenUpload={() => setIsUploadOpen(true)}
        onOpenTechDocs={() => setIsTechDocsOpen(true)}
        totalDocsCount={documents.length}
        readyToPostCount={readyToPostCount}
      />

      {/* Main App Content Area */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6">
        {/* TAB 1: GMAIL AP INBOX */}
        {activeTab === 'inbox' && (
          <GmailInboxPanel
            userEmail={user?.email || null}
            accessToken={accessToken}
            onLogin={handleLogin}
            onConnectDefaultMailbox={handleConnectDefaultMailbox}
            onIngestDocument={handleIngestDocument}
            onSelectDocumentForReview={(doc) => setSelectedDoc(doc)}
          />
        )}

        {/* TAB 2: DOCUMENTS REPOSITORY & CLASSIFICATION */}
        {activeTab === 'documents' && (
          <div className="space-y-4">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div>
                <h1 className="text-xl font-black text-slate-900 tracking-tight">
                  Accounts Payable Documents Hub
                </h1>
                <p className="text-xs text-slate-500">
                  AI-classified repository for Singapore supplier Invoices, Delivery Orders, SOAs, and Credit Notes.
                </p>
              </div>

              <div className="flex items-center space-x-2">
                <button
                  onClick={() => setIsUploadOpen(true)}
                  className="flex items-center space-x-1.5 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-bold shadow-xs transition"
                >
                  <Plus className="w-4 h-4" />
                  <span>Scan New Document</span>
                </button>
              </div>
            </div>

            <DocumentList
              documents={documents}
              onSelectDocument={(doc) => setSelectedDoc(doc)}
              onPostToXero={handlePostToXero}
              onOpenThreeWayMatch={(doc) => {
                setActiveTab('matching');
              }}
            />
          </div>
        )}

        {/* TAB 3: 3-WAY MATCH STUDIO */}
        {activeTab === 'matching' && (
          <ThreeWayMatchStudio
            documents={documents}
            onPostToXero={handlePostToXero}
            onSelectDocument={(doc) => setSelectedDoc(doc)}
          />
        )}

        {/* TAB 4: XERO INTEGRATION */}
        {activeTab === 'xero' && (
          <XeroIntegrationPanel
            documents={documents}
            onBatchPost={handleBatchPostToXero}
            onPostSingle={handlePostToXero}
            onSelectDocument={(doc) => setSelectedDoc(doc)}
          />
        )}

        {/* TAB 5: AP ANALYTICS & CASHFLOW */}
        {activeTab === 'analytics' && (
          <APAnalyticsPanel
            documents={documents}
            onSelectDocument={(doc) => setSelectedDoc(doc)}
          />
        )}
      </main>

      {/* Document Inspector & Detail Modal */}
      <DocumentDetailModal
        document={selectedDoc}
        onClose={() => setSelectedDoc(null)}
        onPostToXero={handlePostToXero}
        onUpdateLineItem={handleUpdateLineItems}
        onApproveDocument={handleApproveDocument}
        onOpenThreeWayMatch={() => {
          setSelectedDoc(null);
          setActiveTab('matching');
        }}
        onOpenUpload={() => {
          setIsUploadOpen(true);
        }}
      />

      {/* Document Upload Modal */}
      <DocumentUploadModal
        isOpen={isUploadOpen}
        onClose={() => setIsUploadOpen(false)}
        onDocumentExtracted={(newDoc) => {
          handleIngestDocument(newDoc);
          setSelectedDoc(newDoc);
        }}
      />

      {/* Technical Design Document (HTML) Modal */}
      <TechDesignDocModal
        isOpen={isTechDocsOpen}
        onClose={() => setIsTechDocsOpen(false)}
      />

      {/* Firebase Auth Domain Guidance Modal */}
      <FirebaseAuthDomainModal
        isOpen={isAuthDomainModalOpen}
        onClose={() => setIsAuthDomainModalOpen(false)}
        onRetryLogin={handleLogin}
        onEnableDemoMode={handleEnableDemoMode}
      />

      {/* Toast Notification Notification */}
      {toast && (
        <div className="fixed bottom-6 right-6 z-50 animate-in slide-in-from-bottom-5 duration-200">
          <div
            className={`flex items-center space-x-2.5 px-4 py-3 rounded-xl shadow-xl border text-xs font-semibold ${
              toast.type === 'success'
                ? 'bg-emerald-950 text-emerald-100 border-emerald-800'
                : toast.type === 'error'
                ? 'bg-rose-950 text-rose-100 border-rose-800'
                : 'bg-slate-900 text-slate-100 border-slate-700'
            }`}
          >
            {toast.type === 'success' ? (
              <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
            ) : toast.type === 'error' ? (
              <AlertCircle className="w-4 h-4 text-rose-400 shrink-0" />
            ) : (
              <Sparkles className="w-4 h-4 text-amber-400 shrink-0" />
            )}
            <span>{toast.message}</span>
          </div>
        </div>
      )}
    </div>
  );
}
