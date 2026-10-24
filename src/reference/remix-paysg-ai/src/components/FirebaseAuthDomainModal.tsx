import React, { useState } from 'react';
import {
  ShieldAlert,
  ExternalLink,
  Copy,
  Check,
  X,
  HelpCircle,
  Sparkles,
  ArrowRight,
  ShieldCheck,
} from 'lucide-react';

interface FirebaseAuthDomainModalProps {
  isOpen: boolean;
  onClose: () => void;
  onRetryLogin: () => void;
  onEnableDemoMode: () => void;
}

export const FirebaseAuthDomainModal: React.FC<FirebaseAuthDomainModalProps> = ({
  isOpen,
  onClose,
  onRetryLogin,
  onEnableDemoMode,
}) => {
  const [copied, setCopied] = useState(false);

  if (!isOpen) return null;

  const currentHostname = typeof window !== 'undefined' ? window.location.hostname : '';
  const currentOrigin = typeof window !== 'undefined' ? window.location.origin : '';
  const projectId = 'gen-lang-client-0169236757';
  const consoleSettingsUrl = `https://console.firebase.google.com/project/${projectId}/authentication/settings`;

  const handleCopyHostname = () => {
    if (currentHostname) {
      navigator.clipboard.writeText(currentHostname);
      setCopied(true);
      setTimeout(() => setCopied(false), 2500);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-950/75 backdrop-blur-xs flex items-center justify-center p-3 sm:p-5 animate-in fade-in duration-150">
      <div className="bg-white rounded-3xl shadow-2xl border border-slate-200 w-full max-w-xl overflow-hidden">
        {/* Header */}
        <div className="p-5 bg-slate-900 text-white flex items-center justify-between border-b border-slate-800">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 bg-amber-500/20 text-amber-400 rounded-xl border border-amber-500/30">
              <ShieldAlert className="w-5 h-5" />
            </div>
            <div>
              <h3 className="font-bold text-sm sm:text-base text-white">
                Authorize Domain in Firebase
              </h3>
              <p className="text-xs text-slate-400 mt-0.5">
                Error: <code className="font-mono text-amber-300">auth/unauthorized-domain</code>
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

        {/* Body */}
        <div className="p-6 space-y-5 text-xs text-slate-700">
          {/* Explanation */}
          <div className="p-3.5 bg-amber-50 border border-amber-200 rounded-xl text-amber-900 space-y-1.5 leading-relaxed">
            <div className="font-bold flex items-center space-x-1.5">
              <span>Why is this happening?</span>
            </div>
            <p>
              Google Firebase enforces security by only allowing OAuth Sign-Ins from domains
              explicitly added to your project's <strong>Authorized domains</strong> allowlist.
              Your newly published domain is not on the list yet.
            </p>
          </div>

          {/* Current Domain Box */}
          <div className="space-y-1.5">
            <label className="text-[11px] font-bold uppercase tracking-wider text-slate-500">
              Your Published Domain to Add:
            </label>
            <div className="flex items-center justify-between p-2.5 bg-slate-100 rounded-xl border border-slate-200 font-mono text-xs text-slate-900 select-all">
              <span className="truncate mr-2 font-semibold">{currentHostname}</span>
              <button
                onClick={handleCopyHostname}
                className="flex items-center space-x-1 px-2.5 py-1 bg-white hover:bg-slate-50 border border-slate-300 rounded-lg text-slate-700 font-sans text-xs font-semibold shrink-0 transition shadow-2xs"
              >
                {copied ? (
                  <>
                    <Check className="w-3.5 h-3.5 text-emerald-600" />
                    <span className="text-emerald-700">Copied!</span>
                  </>
                ) : (
                  <>
                    <Copy className="w-3.5 h-3.5 text-slate-500" />
                    <span>Copy Domain</span>
                  </>
                )}
              </button>
            </div>
          </div>

          {/* Step by step fix */}
          <div className="space-y-2.5">
            <div className="font-bold text-slate-900">How to fix in 30 seconds (no code changes needed):</div>
            <ol className="space-y-2 list-decimal list-inside text-slate-600 leading-normal pl-1">
              <li>
                Open your project in the{' '}
                <a
                  href={consoleSettingsUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-600 font-semibold hover:underline inline-flex items-center space-x-0.5"
                >
                  <span>Firebase Console Settings</span>
                  <ExternalLink className="w-3 h-3 inline" />
                </a>
              </li>
              <li>
                Click on the <strong>Authorized domains</strong> tab and click <strong>Add domain</strong>.
              </li>
              <li>
                Paste <code className="bg-slate-100 px-1.5 py-0.5 rounded font-mono text-slate-800">{currentHostname}</code> and click <strong>Save</strong>.
              </li>
              <li>
                Changes apply instantly. Return here and click <strong>Try Sign In Again</strong>.
              </li>
            </ol>
          </div>

          {/* Quick link button */}
          <div className="pt-1">
            <a
              href={consoleSettingsUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="w-full py-2.5 px-4 bg-blue-600 hover:bg-blue-500 text-white rounded-xl font-bold flex items-center justify-center space-x-2 shadow-xs transition"
            >
              <span>Open Firebase Console Authorized Domains</span>
              <ExternalLink className="w-4 h-4" />
            </a>
          </div>

          {/* Alternative Demo Mode */}
          <div className="p-3.5 bg-emerald-50/70 border border-emerald-200 rounded-xl space-y-2 text-emerald-950">
            <div className="flex items-center justify-between">
              <span className="font-bold flex items-center space-x-1.5 text-emerald-900">
                <Sparkles className="w-4 h-4 text-emerald-600" />
                <span>Want to test immediately without configuring Firebase right now?</span>
              </span>
            </div>
            <p className="text-[11px] text-emerald-800">
              You can connect immediately to the default testing mailbox (<strong>smu.ilp.team5@gmail.com</strong>). It connects as the authorized
              finance officer so you can test Gmail message scanning, AI classification, 3-way matching,
              and Xero bill sync instantly.
            </p>
            <button
              onClick={() => {
                onEnableDemoMode();
                onClose();
              }}
              className="w-full py-2 px-3 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg font-bold text-xs shadow-xs transition flex items-center justify-center space-x-1.5"
            >
              <ShieldCheck className="w-3.5 h-3.5" />
              <span>Connect smu.ilp.team5@gmail.com Now</span>
            </button>
          </div>
        </div>

        {/* Footer */}
        <div className="p-4 bg-slate-50 border-t border-slate-200 flex items-center justify-between">
          <button
            onClick={onClose}
            className="px-4 py-2 text-slate-600 hover:text-slate-900 text-xs font-semibold rounded-lg hover:bg-slate-200 transition"
          >
            Dismiss
          </button>
          <button
            onClick={() => {
              onClose();
              onRetryLogin();
            }}
            className="px-4 py-2 bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold rounded-lg shadow-xs transition flex items-center space-x-1.5"
          >
            <span>Try Sign In Again</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
    </div>
  );
};
