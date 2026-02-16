using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Monolith.DonationPolling.PollDonations
{

    /// <summary>
    /// Handles storing donation images locally and maintaining metadata about stored images.
    /// </summary>
    public class DonationImageStorage
    {
        private readonly DonationDataPaths dataPaths;

        public DonationImageStorage(DonationDataPaths dataPaths)
        {
            this.dataPaths = dataPaths;
        }

        /// <summary>
        /// Retrieves the raw bytes of an image by URL.
        /// </summary>
        public byte[] GetImage(string url)
        {
            string imagePath = GetImagePath(url);
            if (imagePath == null)
            {
                return null;
            }

            return File.ReadAllBytes(imagePath);
        }

        /// <summary>
        /// Retrieves the full path of an image file by URL.
        /// Returns null if image does not exist.
        /// </summary>
        public string GetImagePath(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }
            string imageMetadataFile = dataPaths.GetStoredImagesMetadataFile();
            if (!File.Exists(imageMetadataFile))
            {
                return null;
            }

            try
            {
                string metadataJson = File.ReadAllText(imageMetadataFile);
                var storedImages = JsonConvert.DeserializeObject<StoredImages>(metadataJson) ?? new StoredImages();

                if (storedImages.Images != null && storedImages.Images.TryGetValue(url, out string fileName))
                {
                    string fullyQualifiedFileName = Path.Combine(dataPaths.LatestImageDonationsPath, fileName);

                    if (File.Exists(fullyQualifiedFileName))
                    {
                        return fullyQualifiedFileName;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed find file for image url: {url}: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Checks if a URL exists in metadata and the image file is present on disk.
        /// </summary>
        public bool ContainsAsync(string url)
        {
            string path = GetImagePath(url);
            bool exists = !string.IsNullOrEmpty(path) && File.Exists(path);
            return exists;
        }
    }

}
/// <summary>
/// Represents metadata for stored donation images.
/// Maps original image URLs to locally stored filenames.
/// </summary>
public class StoredImages
{
    /// <summary>
    /// Dictionary mapping image URL → stored filename.
    /// </summary>
    public Dictionary<string, string> Images { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Timestamp of the last metadata update.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }
}
