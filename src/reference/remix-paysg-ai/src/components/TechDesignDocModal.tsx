import React from 'react';
import { X, ExternalLink, Printer, Download, BookOpen, Layers, ShieldCheck, Sparkles, CheckCircle2 } from 'lucide-react';

interface TechDesignDocModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const TechDesignDocModal: React.FC<TechDesignDocModalProps> = ({ isOpen, onClose }) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-950/70 backdrop-blur-xs flex items-center justify-center p-3 sm:p-5">
      <div className="bg-white rounded-3xl shadow-2xl border border-slate-200 w-full max-w-5xl max-h-[92vh] flex flex-col overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        
        {/* Modal Top Bar */}
        <div className="p-4 sm:p-5 bg-slate-900 text-white flex items-center justify-between border-b border-slate-800">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 bg-emerald-500/20 text-emerald-400 rounded-xl border border-emerald-500/30">
              <BookOpen className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h3 className="font-bold text-sm sm:text-base text-white">PaySG AI — Technical Design Document</h3>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 uppercase">
                  Online HTML View
                </span>
              </div>
              <p className="text-xs text-slate-400 mt-0.5">
                Full-stack Architecture, Multimodal Gemini 3.8 Flash Engine, and IRAS 9% GST Validation
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <a
              href="/tech-design-doc.html"
              target="_blank"
              rel="noopener noreferrer"
              className="hidden sm:flex items-center space-x-1.5 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-lg text-xs font-semibold border border-slate-700 transition"
              title="Open full-page HTML in a new browser tab"
            >
              <ExternalLink className="w-3.5 h-3.5 text-emerald-400" />
              <span>Open in New Tab</span>
            </a>

            <button
              onClick={onClose}
              className="p-1.5 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Modal Body: Embedded iframe or rich rendered HTML */}
        <div className="flex-1 overflow-y-auto">
          <iframe
            src="/tech-design-doc.html"
            title="PaySG AI Technical Design Document"
            className="w-full h-[76vh] border-none"
          />
        </div>

        {/* Modal Bottom Bar */}
        <div className="p-3 bg-slate-50 border-t border-slate-200 flex flex-wrap items-center justify-between gap-3 text-xs">
          <div className="flex items-center space-x-2 text-slate-600">
            <span className="w-2 h-2 rounded-full bg-emerald-500"></span>
            <span>Document URL: <code className="font-mono text-slate-800 font-semibold bg-white px-1.5 py-0.5 rounded border border-slate-200">/tech-design-doc.html</code> or <code className="font-mono text-slate-800 font-semibold bg-white px-1.5 py-0.5 rounded border border-slate-200">/docs</code></span>
          </div>

          <div className="flex items-center space-x-2">
            <a
              href="/api/download-tdd-word"
              download="PaySG_AI_Technical_Design_Document.docx"
              className="flex items-center space-x-1 px-3 py-1.5 bg-white border border-slate-300 hover:bg-slate-100 text-slate-700 rounded-lg font-semibold shadow-2xs"
            >
              <Download className="w-3.5 h-3.5 text-blue-600" />
              <span>Download .docx</span>
            </a>

            <button
              onClick={onClose}
              className="px-4 py-1.5 bg-slate-900 hover:bg-slate-800 text-white rounded-lg font-semibold shadow-xs transition"
            >
              Close
            </button>
          </div>
        </div>

      </div>
    </div>
  );
};
