using System;

namespace Monolith.DonationPolling.PollDonations
{
    public class DonationStorageDto<T> where T : class
    {
        /// <summary>
        /// Wether the data is up to date
        /// Data may not be up to date if we lose internet connection
        /// </summary>
        public bool IsUpToDate { get; set; }

        /// <summary>
        /// When we last attempted to update the data
        /// </summary>
        public DateTimeOffset LastUpdateAttemptTimestamp { get; set; }

        /// <summary>
        /// Data last was updated
        /// </summary>
        public DateTimeOffset DataTimestamp { get; set; }

        /// <summary>
        /// The actual data
        /// </summary>
        public T Data { get; set; }
    }
}
