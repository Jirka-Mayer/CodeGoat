using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeGoat.Server
{
    /// <summary>
    /// Contains history of old changes that happened to the document
    /// </summary>
    public class DocumentHistory
    {
        /// <summary>
        /// Change coupled together with the time of committing
        /// </summary>
        private class ChangeTime
        {
            public Change change;
            public DateTime time;
        }

        /// <summary>
        /// What's the minimal size of the queue
        /// (this should work with 1 as well, but just to make sure...)
        /// </summary>
        private const int MinimalQueueSize = 20;

        /// <summary>
        /// After how many seconds is a change concidered to be old and ready to be discarded?
        /// </summary>
        private const int ChangeIsOldAfterSeconds = 10;

        /// <summary>
        /// Latest committed changes.
        /// Does not contain the entire history, because it's not needed.
        /// Empty only on a fresh document, then can never get completely empty,
        /// because the current document state would get lost.
        /// </summary>
        private Queue<ChangeTime> changes = new Queue<ChangeTime>();

        /// <summary>
        /// The latest change commited or null
        /// </summary>
        public Change LatestChange => changes.LastOrDefault()?.change;

        /// <summary>
        /// Converts the sequence of changes into a list with the oldest change at the beginning
        /// </summary>
        public List<Change> ToList()
        {
            return changes.Select(ct => ct.change).ToList();
        }

        /// <summary>
        /// Adds a new change to the history
        /// </summary>
        public void Add(Change change)
        {
            changes.Enqueue(new ChangeTime {
                time = DateTime.Now,
                change = change
            });

            ForgetOldChanges();
        }

        /// <summary>
        /// Returns true if the given change is present in the history
        /// </summary>
        /// <param name="changeId">Id of the given change</param>
        public bool Contains(string changeId)
        {
            return changes.Where(ct => ct.change.Id == changeId).Any();
        }

        /// <summary>
        /// Iterates over all changes that come after a given change chronologically
        /// </summary>
        /// <param name="changeId">Id of the given change</param>
        public IEnumerable<Change> IterateChangesAfter(string changeId)
        {
            bool changeFound = false;

            foreach (var ct in changes)
            {
                if (changeFound)
                {
                    yield return ct.change;
                }
                else
                {
                    if (ct.change.Id == changeId)
                        changeFound = true;
                }
            }
        }

        /// <summary>
        /// Performs a cleanup that gets rid of old chagnes
        /// </summary>
        public void ForgetOldChanges()
        {
            while (IsChangeOld(changes.Peek()))
            {
                changes.Dequeue();
                
                //Console.WriteLine($"Cleaning up change {changes.Peek().change.Id}, size {changes.Count}");
            }
        }

        private bool IsChangeOld(ChangeTime ct)
        {
            // keep minimum number of changes in the queue
            if (changes.Count <= MinimalQueueSize)
                return false;

            return (DateTime.Now - ct.time).TotalSeconds > ChangeIsOldAfterSeconds;
        }
    }
}
