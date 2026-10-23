// Copyright 2026 Spellbound Studio Inc.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Spellbound.Core.Logging {
    public class FileSink : ILogSink, IDisposable {
        private const string DisplayNameValue = "Write To File";

        private const string DefaultLogFileName = "spellbound.log";
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
        private const string ThreadName = "Spellbound.FileSink";
        private const int MaxRotatedFiles = 3;
        private const int FlushIntervalMs = 1000;
        private const int BufferSize = 8192;
        private const int ShutdownJoinTimeoutMs = 2000;

        private string _logFileName;
        private ConcurrentQueue<LogEntry> _queue;
        private ManualResetEventSlim _signal;
        private CancellationTokenSource _shutdownCts;
        private Thread _writerThread;
        private StreamWriter _writer;
        private volatile bool _initialized;

        public string DisplayName => DisplayNameValue;

        public void Initialize(LogConfig config) {
            if (_initialized)
                return;

            _logFileName = string.IsNullOrWhiteSpace(config.logFileName)
                    ? DefaultLogFileName
                    : config.logFileName;

            var directory = Application.persistentDataPath;
            var currentPath = Path.Combine(directory, _logFileName);

            try {
                RotateFiles(directory, _logFileName);

                var stream = new FileStream(
                    currentPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    BufferSize,
                    FileOptions.SequentialScan
                );

                _writer = new StreamWriter(stream) { AutoFlush = false };
                _queue = new ConcurrentQueue<LogEntry>();
                _signal = new ManualResetEventSlim(false);
                _shutdownCts = new CancellationTokenSource();

                _writerThread = new Thread(WriterLoop) {
                    Name = ThreadName,
                    IsBackground = true,
                    Priority = System.Threading.ThreadPriority.BelowNormal
                };
                _writerThread.Start();

                Application.quitting += OnApplicationQuitting;
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

                _initialized = true;
            }
            catch (Exception ex) {
                Debug.LogError($"[FileSink] Failed to initialize: {ex.Message}");
            }
        }

        public void Emit(LogLevel level, string source, string message, string member, int line) {
            if (!_initialized)
                return;

            _queue.Enqueue(new LogEntry {
                Level = level,
                Source = source,
                Message = message,
                Member = member,
                Line = line,
                Timestamp = DateTime.UtcNow
            });

            if (level >= LogLevel.Warning)
                _signal.Set();
        }

        private void WriterLoop() {
            try {
                while (!_shutdownCts.IsCancellationRequested) {
                    _signal.Wait(FlushIntervalMs, _shutdownCts.Token);
                    _signal.Reset();
                    DrainQueue();
                }
            }
            catch (OperationCanceledException) {
                // Expected on shutdown.
            }
            catch (Exception ex) {
                Debug.LogError($"[FileSink] Writer thread crashed: {ex.Message}");
            }
            finally {
                DrainQueue();
            }
        }

        private void DrainQueue() {
            var wroteAnything = false;

            while (_queue.TryDequeue(out var entry)) {
                _writer.WriteLine(FormatEntry(entry));
                wroteAnything = true;
            }

            if (wroteAnything)
                _writer.Flush();
        }

        private static string FormatEntry(LogEntry entry) =>
                $"[{entry.Timestamp.ToString(TimestampFormat)}] " +
                $"[{entry.Level}] " +
                $"[{entry.Source}.{entry.Member}:{entry.Line}] " +
                $"{entry.Message}";

        private static void RotateFiles(string directory, string fileName) {
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            // Delete oldest
            var oldest = Path.Combine(directory, $"{baseName}.{MaxRotatedFiles}{extension}");

            if (File.Exists(oldest))
                File.Delete(oldest);

            // Shift rotated files up by one slot
            for (var i = MaxRotatedFiles - 1; i >= 1; i--) {
                var from = Path.Combine(directory, $"{baseName}.{i}{extension}");
                var to = Path.Combine(directory, $"{baseName}.{i + 1}{extension}");

                if (File.Exists(from))
                    File.Move(from, to);
            }

            // Move current session to .1
            var current = Path.Combine(directory, fileName);
            var firstBackup = Path.Combine(directory, $"{baseName}.1{extension}");

            if (File.Exists(current))
                File.Move(current, firstBackup);
        }

        private void OnApplicationQuitting() => Dispose();
        private void OnProcessExit(object sender, EventArgs e) => Dispose();

        public void Dispose() {
            if (!_initialized)
                return;

            _initialized = false;

            Application.quitting -= OnApplicationQuitting;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

            _shutdownCts.Cancel();
            _signal.Set();
            _writerThread?.Join(ShutdownJoinTimeoutMs);

            _writer?.Dispose();
            _writer = null;

            _signal.Dispose();
            _shutdownCts.Dispose();
        }

        private struct LogEntry {
            public LogLevel Level;
            public string Source;
            public string Message;
            public string Member;
            public int Line;
            public DateTime Timestamp;
        }
    }
}