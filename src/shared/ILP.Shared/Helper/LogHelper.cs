using System;
using System.Collections.Generic;
using System.Text;
using Serilog;

namespace ILP.Shared.Helper
{
    public static class LogHelper
    {

        public static void Debug(string traceId, string message)
        {
            Log.Debug(GetLogEntry(traceId, message));
        }


        public static void Info(string traceId, string message)
        {
            Log.Information(GetLogEntry(traceId, message));
        }

        public static void Error(string traceId, Exception ex, string customMessage = "")
        {
            var prefix = string.IsNullOrEmpty(customMessage) ? "" : $"Message: {customMessage},";
            var message = ($"{prefix} Error: {ex.Message}").Trim();
            var msg = GetLogEntry(traceId, message);
            Log.Error(ex, msg);
        }

        public static void Trace(string traceId, string message)
        {
            Log.Verbose(GetLogEntry(traceId, message));
        }

        private static string GetLogEntry(string traceId, string message)
        {
            return $"TraceId: {traceId}| Message:  {message}";
        }
    }
}
