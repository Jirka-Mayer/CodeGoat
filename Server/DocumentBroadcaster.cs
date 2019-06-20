using System;
using System.Threading;

namespace CodeGoat.Server
{
    /// <summary>
    /// Periodically broadcasts document state
    /// </summary>
    public class DocumentBroadcaster
    {
        /// <summary>
        /// Broadcasting period in ms
        /// </summary>
        public const int PeriodMs = 1000 * 30; // 30s

        /// <summary>
        /// The method that performs the broadcast
        /// </summary>
        private readonly Action performBroadcast;

        /// <summary>
        /// Has the broadcaster been started (is it running now?)
        /// Shared, needs to be locked
        /// </summary>
        private bool started = false;
        public bool Started
        {
            get
            {
                lock (syncLock)
                    return started;
            }
        }

        /// <summary>
        /// Is the broadcaster stopping?
        /// Shared, needs to be locked
        /// </summary>
        private bool stopping = false;
        public bool Stopping
        {
            get
            {
                lock (syncLock)
                    return stopping;
            }
        }
        
        /// <summary>
        ///  Lock for accessing shared properties (started, stopping)
        /// </summary>
        private object syncLock = new object();

        /// <summary>
        /// The thread where the triggering loop runs
        /// Shared and mutable, but uses the "stoppingLock"
        /// </summary>
        private Thread triggeringThread;

        public DocumentBroadcaster(Action performBroadcast)
        {
            this.performBroadcast = performBroadcast;
        }

        /// <summary>
        /// Boots up the triggering thread
        /// </summary>
        public void Start()
        {
            lock (syncLock)
            {
                if (started)
                    throw new InvalidOperationException("Broadcaster has already been started.");

                started = true;

                triggeringThread = new Thread(TriggeringLoop);
                triggeringThread.Start();
            }
        }

        /// <summary>
        /// Makes the thread stop (does not happen immediately)
        /// </summary>
        public void Stop()
        {
            lock (syncLock)
            {
                if (!started)
                    return;

                if (stopping)
                    return;

                stopping = true;

                if (triggeringThread != null)
                    triggeringThread.Interrupt();
            }
        }

        private void TriggeringLoop()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(PeriodMs);
                }
                catch (ThreadInterruptedException)
                {
                    lock (syncLock)
                        if (stopping)
                            break;
                }

                performBroadcast();

                lock (syncLock)
                    if (stopping)
                        break;
            }

            lock (syncLock)
            {
                started = false;
                triggeringThread = null;

                stopping = false;
            }
        }
    }
}
