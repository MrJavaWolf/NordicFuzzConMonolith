using System.Text;

namespace Monolith.DonationPolling.PollDonations;

public class DonationDataStorage(
    DonationDataPaths dataPaths,
    ILogger<DonationDataStorage> logger)
{
    public async Task SaveAsync<T>(T? data, string toDirectory, CancellationToken cancellationToken = default) where T : class
    {
        if (!dataPaths.Enable)
        {
            logger.LogInformation("Storage is disabled, will not save type {Type}", typeof(T).FullName);
            return;
        }

        logger.LogInformation("Starting SaveAsync for type {Type}", typeof(T).FullName);

        string currentDataFile = dataPaths.GetCurrentDataFile(toDirectory);
        logger.LogInformation("Resolved current data file path: {FilePath}", currentDataFile);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        DonationStorageDto<T> donationStorageDto;

        // ------------------------------------------------------------
        // New data is provided
        // ------------------------------------------------------------
        if (data != null)
        {
            logger.LogInformation("Data is NOT null. Creating new DonationStorageDto marked as up-to-date.");

            donationStorageDto = new DonationStorageDto<T>()
            {
                Data = data,
                DataTimestamp = now,
                IsUpToDate = true,
                LastUpdateAttemptTimestamp = now,
            };
        }
        // ------------------------------------------------------------
        // Data is null (failed fetch / polling failed)
        // ------------------------------------------------------------
        else
        {
            logger.LogInformation("Data is null. Attempting to load existing storage to mark as not up-to-date.");

            DonationStorageDto<T>? currentStorageDto = null;

            if (File.Exists(currentDataFile))
            {
                logger.LogInformation("Existing data file found at {FilePath}. Attempting to read.", currentDataFile);

                try
                {
                    logger.LogInformation("Reading file {FilePath}", currentDataFile);
                    string currentDataContent = await File.ReadAllTextAsync(currentDataFile, cancellationToken);
                    logger.LogInformation("Successfully read file {FilePath}", currentDataFile);

                    logger.LogInformation("Attempting to deserialize file content.");
                    currentStorageDto = Newtonsoft.Json.JsonConvert
                        .DeserializeObject<DonationStorageDto<T>>(currentDataContent);

                    logger.LogInformation("Successfully deserialized existing storage DTO.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to read or deserialize existing data file at {FilePath}. A new storage DTO will be created.",
                        currentDataFile);
                }
            }
            else
            {
                logger.LogInformation("No existing data file found at {FilePath}.", currentDataFile);
            }

            if (currentStorageDto == null)
            {
                logger.LogInformation("No existing DTO available. Creating new DTO marked as not up-to-date.");

                currentStorageDto = new DonationStorageDto<T>()
                {
                    Data = null,
                    DataTimestamp = null,
                    IsUpToDate = false,
                    LastUpdateAttemptTimestamp = now,
                };
            }
            else
            {
                logger.LogInformation("Existing DTO found. Marking as not up-to-date and updating LastUpdateAttemptTimestamp.");

                currentStorageDto.IsUpToDate = false;
                currentStorageDto.LastUpdateAttemptTimestamp = now;
            }

            donationStorageDto = currentStorageDto;
        }

        // ------------------------------------------------------------
        // Serialize DTO
        // ------------------------------------------------------------
        logger.LogInformation("Serializing...");
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(donationStorageDto, Newtonsoft.Json.Formatting.Indented);
        logger.LogInformation("Serialization completed");

        // ------------------------------------------------------------
        // Ensure directory exists
        // ------------------------------------------------------------
        var directory = Path.GetDirectoryName(currentDataFile);

        if (string.IsNullOrWhiteSpace(directory))
        {
            logger.LogError("Failed to resolve directory for path {FilePath}", currentDataFile);
            throw new Exception($"Failed to get directory for '{currentDataFile}' directory found was an empty string");
        }

        if (!Directory.Exists(directory))
        {
            logger.LogInformation("Directory {Directory} does not exist. Creating directory.", directory);
            Directory.CreateDirectory(directory);
            logger.LogInformation("Directory {Directory} successfully created.", directory);
        }

        // ------------------------------------------------------------
        // Create temp file (prevents half-written files)
        // ------------------------------------------------------------
        var tempFile = Path.Combine(
            directory,
            $"{Path.GetFileName(currentDataFile)}.{Guid.NewGuid()}.tmp");

        logger.LogInformation("Temporary file will be written to {TempFile}", tempFile);

        try
        {
            // Write to temp file
            logger.LogInformation("Writing JSON to temporary file {TempFile}", tempFile);
            await File.WriteAllTextAsync(tempFile, json, Encoding.UTF8, cancellationToken);
            logger.LogInformation("Successfully wrote temporary file {TempFile}", tempFile);

            // Move/replace file atomically
            logger.LogInformation(
                "Replacing destination file {DestinationFile} with temporary file {TempFile}",
                currentDataFile,
                tempFile);

            File.Move(
                sourceFileName: tempFile,
                destFileName: currentDataFile,
                overwrite: true);

            logger.LogInformation("Successfully replaced file {DestinationFile}", currentDataFile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "An error occurred while writing or replacing file {DestinationFile}",
                currentDataFile);
            throw;
        }
        finally
        {
            // Cleanup temp file if something failed
            if (File.Exists(tempFile))
            {
                logger.LogWarning("Temporary file {TempFile} still exists. Attempting cleanup.", tempFile);

                try
                {
                    File.Delete(tempFile);
                    logger.LogInformation("Temporary file {TempFile} deleted successfully.", tempFile);
                }
                catch (Exception cleanupEx)
                {
                    logger.LogError(cleanupEx,
                        "Failed to delete temporary file {TempFile}", tempFile);
                }
            }
        }

        logger.LogInformation("SaveAsync for type {Type} completed successfully.", typeof(T).Name);

        // ------------------------------------------------------------
        // Append to JSONL audit file
        // ------------------------------------------------------------
        var jsonlFile = dataPaths.GetHistoryDataFile(directory);

        try
        {
            logger.LogInformation("Appending entry to JSONL audit file {JsonlFile}", jsonlFile);

            // Ensure directory still exists (defensive)
            if (!Directory.Exists(directory))
            {
                logger.LogWarning("Directory {Directory} missing before JSONL append. Recreating.", directory);
                Directory.CreateDirectory(directory);
            }

            // JSONL requires exactly one JSON object per line
            // We ensure newline separation explicitly.
            var line = Newtonsoft.Json.JsonConvert.SerializeObject(donationStorageDto) + Environment.NewLine;

            await File.AppendAllTextAsync(
                jsonlFile,
                line,
                Encoding.UTF8,
                cancellationToken);

            logger.LogInformation("Successfully appended entry to JSONL file {JsonlFile}", jsonlFile);
        }
        catch (Exception ex)
        {
            // Important design choice:
            // We DO NOT fail the main save operation if audit logging fails.
            logger.LogError(ex,
                "Failed to append JSON entry to JSONL audit file {JsonlFile}.",
                jsonlFile);
        }
    }
}
