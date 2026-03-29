namespace ModelPublisher.ManifestEditor.Models;

public static class ManifestValidator
{
    public static IReadOnlyList<string> Validate(ManifestEditorState state)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(state.Title))
            errors.Add("title is required.");

        if (state.ModelFiles.Count == 0)
            errors.Add("At least one model file is required.");

        foreach (var path in state.ModelFiles.Where(p => !File.Exists(p)))
            errors.Add($"Model file not found: {Path.GetFileName(path)}");

        foreach (var path in state.Photos.Where(p => !File.Exists(p)))
            errors.Add($"Photo not found: {Path.GetFileName(path)}");

        foreach (var p in state.Platforms.Where(p => p.IsEnabled))
        {
            foreach (var profile in p.PrintProfiles.Where(f => !File.Exists(f)))
                errors.Add($"Print profile not found: {Path.GetFileName(profile)}");

            if (p.PlatformKey == "patreon" && p.FreePost == false && string.IsNullOrWhiteSpace(p.AccessTierId))
                errors.Add("Patreon: access_tier_id is required when free_post is false.");
        }

        return errors;
    }
}
