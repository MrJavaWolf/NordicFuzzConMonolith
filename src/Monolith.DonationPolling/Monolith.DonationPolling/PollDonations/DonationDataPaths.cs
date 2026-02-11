namespace Monolith.DonationPolling.PollDonations
{
    public class DonationDataPaths
    {
        public required bool Enable { get; set; }
        public required string Path { get; set; }

        public string PortalCharityDonationsPath => System.IO.Path.Combine(Path, "portal_charity_donations");
        public string MonetaryStatusPath => System.IO.Path.Combine(Path, "monetary_status");
        public string BiggestDonationsPath => System.IO.Path.Combine(Path, "biggest_donations");
        public string LatestDonationsPath => System.IO.Path.Combine(Path, "latest_donations");
        public string BiggestDonationStatisticsPath => System.IO.Path.Combine(Path, "biggest_donation_statistics");
        public string LatestImageDonationsPath => System.IO.Path.Combine(Path, "latest_images_donations");
        public string GetCurrentDataFile(string path) => System.IO.Path.Combine(path, "current.json");
        public string GetHistoryDataFile(string path) => System.IO.Path.Combine(path, "history.jsonl");
        public string GetStoredImagesMetadataFile() => System.IO.Path.Combine(LatestImageDonationsPath, "images_metadata.json");
    }
}
