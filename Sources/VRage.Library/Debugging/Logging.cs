using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace VRage.Library.Debugging
{

    public interface ILogEvent
    {
        void Write(TextWriter writer);
    }

    public static class StubbedLogging
    {
        public static void Text(string format, params object[] args)
        {
        }

        public static void Event(ILogEvent e)
        {
        }
    }

    public static class Logging
    {
        private static string ROOTDIR = "e:\\se-log";

        private static string eventLog = Path.Combine(ROOTDIR, "events.csv");
        private static string textLog = Path.Combine(ROOTDIR, "log.txt");

        private static readonly ConcurrentQueue<ILogEvent> events = new ConcurrentQueue<ILogEvent>();
        private static readonly ConcurrentQueue<string> text = new ConcurrentQueue<string>();

        private static int flushRequested;
        private static ManualResetEventSlim flushEvent = new ManualResetEventSlim();
        
        public static void Text(string format, params object[] args)
        {
            text.Enqueue(String.Concat(DateTimeOffset.UtcNow, " [", Thread.CurrentThread.ManagedThreadId, "] ", String.Format(format, args)));
            EnsureLogThreadIsRunning();
        }

        public static void Event(ILogEvent e)
        {
            events.Enqueue(e);
            EnsureLogThreadIsRunning();
        }

        private static void DumpEvents()
        {
            if(!Directory.Exists(ROOTDIR)) Directory.CreateDirectory(ROOTDIR);
            if(File.Exists(eventLog)) File.Delete(eventLog);
            if(File.Exists(textLog)) File.Delete(textLog);

            using(var eventLogWriter = Open(eventLog))
            {
                using(var textLogWriter = Open(textLog))
                {
                    while(true)
                    {
                        var written = false;
                        ILogEvent info;
                        if(events.TryDequeue(out info))
                        {
                            info.Write(eventLogWriter);
                            written = true;
                        }
                        string line;
                        if(text.TryDequeue(out line))
                        {
                            textLogWriter.WriteLine(line);
                            written = true;
                        }
                        if(!written) Thread.Sleep(500);
                        if(Interlocked.CompareExchange(ref flushRequested, 0, 1) == 1)
                        {
                            textLogWriter.Flush();
                            eventLogWriter.Flush();
                            flushEvent.Set();
                        }
                    }
                }
            }
        }

        private static TextWriter Open(string file)
        {
            var stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read);
            return new StreamWriter(stream);
        }
        
        public static void Flush()
        {
            if(consumer.ThreadState == System.Threading.ThreadState.Running)
            {
                flushEvent.Reset();
                Interlocked.Exchange(ref flushRequested, 1);
                flushEvent.Wait();
            }
        }

        private static void EnsureLogThreadIsRunning()
        {
            
            if(consumer.ThreadState == System.Threading.ThreadState.Unstarted)
            lock(consumer)
            {
                if(consumer.ThreadState == System.Threading.ThreadState.Unstarted) consumer.Start();
            }
        }

        private static readonly Thread consumer = new Thread(DumpEvents);

    }

}
