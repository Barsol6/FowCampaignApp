namespace FowCampaign.Api.Modules.Storage;

public static class MapRecovery
{
    public static void RestoreMissingMaps(string contentRoot, string mapsDirectory, ILogger logger)
    {
        var recoveryDirectory = Path.Combine(contentRoot, "RecoveryMaps");
        if (!Directory.Exists(recoveryDirectory)) return;

        foreach (var source in Directory.EnumerateFiles(recoveryDirectory, "*.png"))
        {
            var destination = Path.Combine(mapsDirectory, Path.GetFileName(source));
            if (File.Exists(destination)) continue;

            Directory.CreateDirectory(mapsDirectory);
            // Publish the complete image atomically, without replacing existing uploads.
            var temporaryPath = Path.Combine(mapsDirectory, $".recovery-{Guid.NewGuid()}.tmp");
            try
            {
                File.Copy(source, temporaryPath);
                try
                {
                    File.Move(temporaryPath, destination, overwrite: false);
                    logger.LogInformation("Restored missing campaign map: {MapPath}", destination);
                }
                catch (IOException) when (File.Exists(destination))
                {
                    // Another instance may have restored the same map first.
                }
            }
            finally
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
