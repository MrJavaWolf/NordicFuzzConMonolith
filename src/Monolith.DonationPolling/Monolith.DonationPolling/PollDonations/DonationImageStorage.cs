using Newtonsoft.Json;

namespace Monolith.DonationPolling.PollDonations;

/// <summary>
/// Handles storing donation images locally and maintaining metadata about stored images.
/// </summary>
public class DonationImageStorage(DonationDataPaths dataPaths, ILogger<DonationImageStorage> logger)
{

    /// <summary>
    /// Saves an image to disk if it has not already been stored.
    /// Also updates the metadata file containing URL-to-file mappings.
    /// </summary>
    public async Task SaveImage(string url, byte[] imageBytes, CancellationToken cancellationToken)
    {

        // Feature toggle — exit early if disabled
        if (!dataPaths.Enable)
        {
            logger.LogInformation("Image storage is disabled. Skipping SaveImage for URL: {Url}", url);
            return;
        }

        logger.LogInformation("Starting SaveImage for URL: {Url}, number of bytes: {bytes}", url, imageBytes.Length);


        // Directory where images are stored
        string imageDirectory = dataPaths.LatestImageDonationsPath;

        // Metadata file path
        string imageMetadataFile = dataPaths.GetStoredImagesMetadataFile();

        logger.LogInformation("Using image directory: {Directory}", imageDirectory);
        logger.LogInformation("Using metadata file: {MetadataFile}", imageMetadataFile);

        StoredImages? storedImages = null;

        // Attempt to load existing metadata file
        if (File.Exists(imageMetadataFile))
        {
            logger.LogInformation("Metadata file exists. Reading metadata from disk.");
            try
            {
                string metadataJson = await File.ReadAllTextAsync(imageMetadataFile, cancellationToken);
                logger.LogInformation("Successfully read metadata file.");

                storedImages = JsonConvert.DeserializeObject<StoredImages>(metadataJson);

                logger.LogInformation("Successfully deserialized metadata.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read the deserialized metadata. Will create a new one");
            }
        }
        else
        {
            logger.LogInformation("Metadata file does not exist. A new one will be created.");
        }

        // Initialize metadata object if null
        if (storedImages == null)
        {
            logger.LogInformation("Initializing new StoredImages metadata object.");

            storedImages = new StoredImages()
            {
                LastUpdated = DateTimeOffset.UtcNow
            };
        }

        // Ensure dictionary exists
        if (storedImages.Images == null)
        {
            logger.LogWarning("StoredImages.Images was null. Initializing empty dictionary.");
            storedImages.Images = [];
        }

        // Check if image already exists and file is present
        if (storedImages.Images.ContainsKey(url) &&
            File.Exists(Path.Combine(imageDirectory, storedImages.Images[url])))
        {
            logger.LogInformation("Image already stored for URL: {Url}. Skipping write.", url);
            return;
        }

        // Ensure image directory exists
        if (!Directory.Exists(imageDirectory))
        {
            logger.LogWarning("Directory {Directory} missing for images. Recreating.", imageDirectory);
            Directory.CreateDirectory(imageDirectory);
            logger.LogInformation("Directory {Directory} successfully created.", imageDirectory);
        }

        // Extract the file extension from the URL
        string extension = Path.GetExtension(url);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png"; // fallback if URL has no extension
        }

        // Generate unique filename using the URL's extension
        string fileName = Guid.NewGuid() + extension;
        string fullyQualifiedFileName = Path.Combine(imageDirectory, fileName);

        // Write image to disk
        logger.LogInformation("Writing image file to {FilePath}", fullyQualifiedFileName);
        await File.WriteAllBytesAsync(fullyQualifiedFileName, imageBytes, cancellationToken);
        logger.LogInformation("Successfully wrote image file to {FilePath}", fullyQualifiedFileName);

        // Add or update metadata entry
        if (!storedImages.Images.ContainsKey(url))
        {
            logger.LogInformation("Adding new metadata entry for URL: {Url}", url);
            storedImages.Images.Add(url, fileName);
        }
        else
        {
            logger.LogInformation("Updating existing metadata entry for URL: {Url}", url);
            storedImages.Images[url] = fileName;
        }

        // Update last modified timestamp
        storedImages.LastUpdated = DateTimeOffset.UtcNow;

        // Serialize metadata
        logger.LogInformation("Serializing metadata to JSON.");
        string serializedJson = JsonConvert.SerializeObject(storedImages, Formatting.Indented);

        // Write metadata file (FIXED: writing to metadata file, not image file)
        logger.LogInformation("Writing metadata file to {MetadataFile}", imageMetadataFile);
        await File.WriteAllTextAsync(imageMetadataFile, serializedJson, cancellationToken);
        logger.LogInformation("Successfully wrote metadata file.");

        logger.LogInformation("SaveImage completed for URL: {Url}", url);
    }

    /// <summary>
    /// Retrieves the raw bytes of an image by URL.
    /// </summary>
    public async Task<byte[]?> GetImage(string url, CancellationToken cancellationToken)
    {
        string? imagePath = await GetImagePathAsync(url, cancellationToken);
        if (imagePath == null)
        {
            logger.LogWarning("GetImage: Image not found for URL: {Url}", url);
            return null;
        }

        logger.LogInformation("Reading image bytes from {FilePath}", imagePath);
        return await File.ReadAllBytesAsync(imagePath, cancellationToken);
    }

    /// <summary>
    /// Retrieves the full path of an image file by URL.
    /// Returns null if image does not exist.
    /// </summary>
    public async Task<string?> GetImagePathAsync(string url, CancellationToken cancellationToken)
    {
        string imageMetadataFile = dataPaths.GetStoredImagesMetadataFile();
        if (!File.Exists(imageMetadataFile))
        {
            logger.LogWarning("Metadata file {MetadataFile} does not exist.", imageMetadataFile);
            return null;
        }

        try
        {
            string metadataJson = await File.ReadAllTextAsync(imageMetadataFile, cancellationToken);
            var storedImages = JsonConvert.DeserializeObject<StoredImages>(metadataJson) ?? new StoredImages();

            if (storedImages.Images != null && storedImages.Images.TryGetValue(url, out string? fileName))
            {
                string fullyQualifiedFileName = Path.Combine(dataPaths.LatestImageDonationsPath, fileName);

                if (File.Exists(fullyQualifiedFileName))
                {
                    logger.LogInformation("Image found for URL: {Url}, path: {FilePath}", url, fullyQualifiedFileName);
                    return fullyQualifiedFileName;
                }
                else
                {
                    logger.LogWarning("Image file {FilePath} missing for URL: {Url}", fullyQualifiedFileName, url);
                    return null;
                }
            }
            else
            {
                logger.LogInformation("No metadata entry found for URL: {Url}", url);
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read or parse metadata file: {MetadataFile}", imageMetadataFile);
            return null;
        }
    }

    /// <summary>
    /// Checks if a URL exists in metadata and the image file is present on disk.
    /// </summary>
    public async Task<bool> ContainsAsync(string url, CancellationToken cancellationToken)
    {
        string? path = await GetImagePathAsync(url, cancellationToken);
        bool exists = !string.IsNullOrEmpty(path) && File.Exists(path);
        logger.LogInformation("Contains check for URL: {Url}, exists: {Exists}", url, exists);
        return exists;
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
    public Dictionary<string, string> Images { get; set; } = [];

    /// <summary>
    /// Timestamp of the last metadata update.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }
}
