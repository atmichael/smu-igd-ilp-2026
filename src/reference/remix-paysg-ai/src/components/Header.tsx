import React from 'react';
import {
  FileText,
  Mail,
  RefreshCw,
  Upload,
  Layers,
  BarChart3,
  Building2,
  LogOut,
  BookOpen,
} from 'lucide-react';
import { User } from '../services/auth';

interface HeaderProps {
  activeTab: 'inbox' | 'documents' | 'matching' | 'xero' | 'analytics';
  setActiveTab: (tab: 'inbox' | 'documents' | 'matching' | 'xero' | 'analytics') => void;
  user: User | null;
  onLogin: () => void;
  onLogout: () => void;
  onConnectDefaultMailbox?: () => void;
  isLoggingIn: boolean;
  onOpenUpload: () => void;
  onOpenTechDocs?: () => void;
  totalDocsCount: number;
  readyToPostCount: number;
}

export const Header: React.FC<HeaderProps> = ({
  activeTab,
  setActiveTab,
  user,
  onLogin,
  onLogout,
  onConnectDefaultMailbox,
  isLoggingIn,
  onOpenUpload,
  onOpenTechDocs,
  totalDocsCount,
  readyToPostCount,
}) => {
  return (
    <header className="bg-slate-900 text-white border-b border-slate-800 sticky top-0 z-40 shadow-md">
      {/* Tier 1: Main Brand Bar & Global Actions */}
      <div className="max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16 gap-3">
          {/* Left: Brand Identity */}
          <div className="flex items-center space-x-3 shrink-0">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-emerald-600 to-teal-400 flex items-center justify-center shadow-lg shadow-emerald-950/50 shrink-0">
              <Building2 className="w-5 h-5 text-white" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="font-extrabold text-lg sm:text-xl tracking-tight text-white">
                  PaySG AI
                </span>
                <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 whitespace-nowrap">
                  Singapore AP
                </span>
                <span className="hidden sm:inline-block text-[10px] font-semibold px-1.5 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700 whitespace-nowrap">
                  IRAS 9% GST
                </span>
              </div>
              <p className="hidden md:block text-[11px] text-slate-400 leading-tight mt-0.5">
                AI Accounts Payable Automation &amp; 3-Way Match
              </p>
            </div>
          </div>

          {/* Right: Global Action Buttons (Guaranteed space, never clipped) */}
          <div className="flex items-center space-x-2 sm:space-x-2.5 shrink-0">
            {/* Tech Docs HTML Button */}
            {onOpenTechDocs ? (
              <button
                onClick={onOpenTechDocs}
                className="flex items-center space-x-1.5 px-2.5 sm:px-3 py-1.5 text-xs font-semibold rounded-lg bg-emerald-950/80 text-emerald-300 hover:text-white hover:bg-emerald-900 border border-emerald-500/40 transition shadow-2xs whitespace-nowrap"
                title="View complete Technical Design Document online (HTML)"
              >
                <BookOpen className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                <span className="hidden sm:inline">Tech Docs (HTML)</span>
                <span className="sm:hidden">Docs</span>
              </button>
            ) : (
              <a
                href="/tech-design-doc.html"
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center space-x-1.5 px-2.5 sm:px-3 py-1.5 text-xs font-semibold rounded-lg bg-emerald-950/80 text-emerald-300 hover:text-white hover:bg-emerald-900 border border-emerald-500/40 transition shadow-2xs whitespace-nowrap"
                title="View complete Technical Design Document online (HTML)"
              >
                <BookOpen className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                <span className="hidden sm:inline">Tech Docs (HTML)</span>
                <span className="sm:hidden">Docs</span>
              </a>
            )}

            {/* Upload Document Button */}
            <button
              onClick={onOpenUpload}
              className="flex items-center space-x-1.5 px-3 py-1.5 text-xs font-bold rounded-lg bg-emerald-500 text-slate-950 hover:bg-emerald-400 transition-colors shadow-xs whitespace-nowrap shrink-0"
              title="Upload Singapore Invoice, Delivery Order, SOA or Credit Note"
            >
              <Upload className="w-3.5 h-3.5 shrink-0" />
              <span className="hidden sm:inline">Upload Document</span>
              <span className="sm:hidden">Upload</span>
            </button>

            {/* Gmail Authentication / User Status */}
            {user ? (
              <div className="flex items-center space-x-2 pl-2 border-l border-slate-700/80 shrink-0">
                {user.photoURL ? (
                  <img
                    src={user.photoURL}
                    alt={user.displayName || 'AP Officer'}
                    className="w-7 h-7 rounded-full border border-emerald-400 shrink-0"
                  />
                ) : (
                  <div className="w-7 h-7 rounded-full bg-emerald-700 text-white flex items-center justify-center text-xs font-bold shrink-0">
                    {(user.displayName || user.email || 'S')[0].toUpperCase()}
                  </div>
                )}
                <div className="hidden lg:block text-left text-xs leading-tight">
                  <div
                    className="text-slate-200 font-semibold truncate max-w-[130px]"
                    title={user.email || ''}
                  >
                    {user.email === 'smu.ilp.team5@gmail.com'
                      ? 'SMU ILP Team 5'
                      : user.displayName || user.email?.split('@')[0]}
                  </div>
                  <div className="text-[10px] text-emerald-400 flex items-center space-x-1">
                    <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse"></span>
                    <span title={`Connected to ${user.email}`}>AP Mailbox Active</span>
                  </div>
                </div>
                <button
                  onClick={onLogout}
                  title="Disconnect / Change Mailbox Session"
                  className="p-1.5 text-slate-400 hover:text-red-400 hover:bg-slate-800 rounded-lg transition shrink-0"
                >
                  <LogOut className="w-3.5 h-3.5" />
                </button>
              </div>
            ) : (
              <div className="flex items-center space-x-1.5 shrink-0">
                {onConnectDefaultMailbox && (
                  <button
                    onClick={onConnectDefaultMailbox}
                    className="flex items-center space-x-1.5 bg-emerald-600 hover:bg-emerald-500 text-white px-2.5 sm:px-3 py-1.5 rounded-lg text-xs font-bold transition shadow-xs whitespace-nowrap shrink-0"
                    title="Connect default AP mailbox: smu.ilp.team5@gmail.com"
                  >
                    <Mail className="w-3.5 h-3.5 shrink-0" />
                    <span>Connect AP Mailbox</span>
                  </button>
                )}
                <button
                  onClick={onLogin}
                  disabled={isLoggingIn}
                  className="flex items-center space-x-1.5 bg-white text-slate-900 hover:bg-slate-100 px-2.5 sm:px-3 py-1.5 rounded-lg text-xs font-bold transition shadow-xs border border-slate-300 disabled:opacity-50 whitespace-nowrap shrink-0"
                  title="Connect via Google OAuth popup"
                >
                  <svg className="w-3.5 h-3.5 shrink-0" viewBox="0 0 48 48">
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
                  <span className="hidden sm:inline">
                    {isLoggingIn ? 'Connecting...' : 'Google OAuth'}
                  </span>
                  <span className="sm:hidden">{isLoggingIn ? '...' : 'OAuth'}</span>
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Tier 2: Dedicated Navigation Tab Bar (Clean, Spacious & Beautiful) */}
      <div className="border-t border-slate-800/80 bg-slate-900/95 backdrop-blur-xs">
        <div className="max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center space-x-1 sm:space-x-2 py-2 overflow-x-auto no-scrollbar">
            {/* Tab 1: Gmail AP Inbox */}
            <button
              onClick={() => setActiveTab('inbox')}
              className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all ${
                activeTab === 'inbox'
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'text-slate-300 hover:text-white hover:bg-slate-800/80'
              }`}
            >
              <Mail className="w-3.5 h-3.5 shrink-0" />
              <span>Gmail AP Inbox</span>
              {user && (
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse"></span>
              )}
            </button>

            {/* Tab 2: Documents */}
            <button
              onClick={() => setActiveTab('documents')}
              className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all ${
                activeTab === 'documents'
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'text-slate-300 hover:text-white hover:bg-slate-800/80'
              }`}
            >
              <FileText className="w-3.5 h-3.5 shrink-0" />
              <span>Documents Hub</span>
              <span
                className={`px-1.5 py-0.5 rounded-full text-[10px] font-bold ${
                  activeTab === 'documents'
                    ? 'bg-emerald-700 text-emerald-100'
                    : 'bg-slate-800 text-slate-300 border border-slate-700'
                }`}
              >
                {totalDocsCount}
              </span>
            </button>

            {/* Tab 3: 3-Way Match Studio */}
            <button
              onClick={() => setActiveTab('matching')}
              className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all ${
                activeTab === 'matching'
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'text-slate-300 hover:text-white hover:bg-slate-800/80'
              }`}
            >
              <Layers className="w-3.5 h-3.5 shrink-0" />
              <span>3-Way Match</span>
              <span
                className={`hidden md:inline-block px-1.5 py-0.5 rounded text-[10px] font-semibold ${
                  activeTab === 'matching'
                    ? 'bg-emerald-700 text-emerald-100'
                    : 'bg-slate-800/80 text-slate-400'
                }`}
              >
                Studio
              </span>
            </button>

            {/* Tab 4: Xero Sync */}
            <button
              onClick={() => setActiveTab('xero')}
              className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all ${
                activeTab === 'xero'
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'text-slate-300 hover:text-white hover:bg-slate-800/80'
              }`}
            >
              <RefreshCw className="w-3.5 h-3.5 shrink-0" />
              <span>Xero Sync</span>
              {readyToPostCount > 0 && (
                <span className="px-1.5 py-0.5 bg-emerald-500 text-slate-950 font-extrabold rounded-full text-[10px]">
                  {readyToPostCount}
                </span>
              )}
            </button>

            {/* Tab 5: AP Analytics & GST */}
            <button
              onClick={() => setActiveTab('analytics')}
              className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all ${
                activeTab === 'analytics'
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'text-slate-300 hover:text-white hover:bg-slate-800/80'
              }`}
            >
              <BarChart3 className="w-3.5 h-3.5 shrink-0" />
              <span>Cashflow &amp; GST</span>
              <span
                className={`hidden sm:inline-block px-1.5 py-0.5 rounded text-[10px] font-medium ${
                  activeTab === 'analytics'
                    ? 'bg-emerald-700 text-emerald-100'
                    : 'bg-slate-800 text-slate-400'
                }`}
              >
                IRAS Form 5
              </span>
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};
