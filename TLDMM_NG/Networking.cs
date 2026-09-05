using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public sealed class ModSummary
{
    [JsonPropertyName("DisplayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonPropertyName("Author")]
    public string? Author { get; set; }

    [JsonPropertyName("DisplayAuthor")]
    public List<string>? DisplayAuthor { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("Aliases")]
    public List<string>? Aliases { get; set; }

    [JsonPropertyName("Replaces")]
    public List<string>? Replaces { get; set; }

    [JsonPropertyName("ModUrl")]
    public string? ModUrl { get; set; }

    [JsonPropertyName("RepoName")]
    public string? RepoName { get; set; }

    [JsonPropertyName("Dependencies")]
    public List<string>? Dependencies { get; set; }

    [JsonPropertyName("Version")]
    public string? Version { get; set; }

    [JsonPropertyName("Error")]
    public bool Error { get; set; }

    [JsonPropertyName("AutoUpdate")]
    public bool AutoUpdate { get; set; }

    [JsonPropertyName("Download")]
    public string? Download { get; set; }

    [JsonPropertyName("Downloads")]
    public List<string>? Downloads { get; set; }

    [JsonPropertyName("AuthorUrl")]
    public string? AuthorUrl { get; set; }

    [JsonPropertyName("SupportUrl")]
    public string? SupportUrl { get; set; }

    [JsonPropertyName("Categories")]
    public List<string>? Categories { get; set; }

    [JsonPropertyName("TestedOn")]
    public string? TestedOn { get; set; }

    [JsonPropertyName("Released")]
    public string? Released { get; set; }

    [JsonPropertyName("Updated")]
    public string? Updated { get; set; }

    [JsonPropertyName("Images")]
    public List<string>? Images { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("PreviousAuthors")]
    public List<string>? PreviousAuthors { get; set; }

    [JsonPropertyName("Source")]
    public string? Source { get; set; }
}

public class SourceModList
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("authorURL")]
    public string? AuthorUrl { get; set; }

    [JsonPropertyName("supportURL")]
    public string? SupportUrl { get; set; }

    [JsonPropertyName("mods")]
    public List<SourceMod>? Mods { get; set; }
}

public class SourceMod
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("previousAuthors")]
    public string? PreviousAuthors { get; set; }

    [JsonPropertyName("released")]
    public string? Released { get; set; }

    [JsonPropertyName("updated")]
    public string? Updated { get; set; }

    [JsonPropertyName("downloadURL")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("modURL")]
    public string? ModUrl { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }

    [JsonPropertyName("gameversion")]
    public List<string>? GameVersion { get; set; }

    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    [JsonPropertyName("dependencies")]
    public List<string>? Dependencies { get; set; }

    [JsonPropertyName("status")]
    public SourceStatus? Status { get; set; }

    [JsonPropertyName("testedon")]
    public SourceTestedOn? TestedOn { get; set; }

    [JsonPropertyName("updater")]
    public SourceUpdater? Updater { get; set; }

    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }

    [JsonPropertyName("replaces")]
    public List<string>? Replaces { get; set; }
}

public class SourceStatus
{
    [JsonPropertyName("working")]
    public bool Working { get; set; }

    [JsonPropertyName("beta")]
    public bool Beta { get; set; }

    [JsonPropertyName("patchnotes")]
    public string? PatchNotes { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("issues")]
    public string? Issues { get; set; }
}

public class SourceTestedOn
{
    [JsonPropertyName("tldversion")]
    public string? TldVersion { get; set; }

    [JsonPropertyName("mlversion")]
    public string? MlVersion { get; set; }
}

public class SourceUpdater
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }

    [JsonPropertyName("downloads")]
    public List<string>? Downloads { get; set; }
}

public class Networking
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private const string ApiUrl = "https://tldmods.net/api.php";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<List<ModSummary>> GetModsAsync()
    {
        try
        {
            string json = await _httpClient.GetStringAsync(ApiUrl);
            var mods = JsonSerializer.Deserialize<List<ModSummary>>(json, JsonOptions);
            return mods ?? new List<ModSummary>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching mods from API: {ex.Message}");
            return new List<ModSummary>();
        }
    }

    private async Task<SourceMod?> GetSourceModAsync(string? sourceUrl, string modName)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl) || string.IsNullOrWhiteSpace(modName))
            return null;

        try
        {
            string json;
            
            string? cachedJson = await GetCachedJsonAsync(sourceUrl);
            if (cachedJson != null)
            {
                json = cachedJson;
            }
            else
            {
                json = await _httpClient.GetStringAsync(sourceUrl);
                await SaveToCacheAsync(sourceUrl, json);
            }

            var sourceList = JsonSerializer.Deserialize<SourceModList>(json, JsonOptions);

            if (sourceList?.Mods == null)
                return null;

            return sourceList.Mods.FirstOrDefault(m =>
                string.Equals(m.Name, modName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching source from {sourceUrl}: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> GetCachedJsonAsync(string url)
    {
        string cacheFilePath = GetCacheFilePath(url);
        if (File.Exists(cacheFilePath))
        {
            return await File.ReadAllTextAsync(cacheFilePath);
        }
        return null;
    }

    private async Task SaveToCacheAsync(string url, string json)
    {
        string cacheFilePath = GetCacheFilePath(url);
        await File.WriteAllTextAsync(cacheFilePath, json);
    }

    private string GetCacheFilePath(string url)
    {
        string hash = ComputeHash(url);
        return Path.Combine(AppData.CacheFolder, $"{hash}.json");
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private ModSummary MergeModData(ModSummary mainMod, SourceMod? sourceMod)
    {
        if (sourceMod == null)
            return mainMod;

        static List<T>? PickList<T>(List<T>? main, List<T>? source)
        {
            if (main is { Count: > 0 }) return main;
            if (source is { Count: > 0 }) return source;
            return main ?? source;
        }

        static string? PickString(string? main, string? source)
        {
            return !string.IsNullOrWhiteSpace(main) ? main : source;
        }

        return new ModSummary
        {
            DisplayName = PickString(mainMod.DisplayName, sourceMod.DisplayName),
            Name = PickString(mainMod.Name, sourceMod.Name),
            Type = PickString(mainMod.Type, sourceMod.Type),
            Author = PickString(mainMod.Author, sourceMod.Author),
            DisplayAuthor = mainMod.DisplayAuthor is { Count: > 0 }
                ? mainMod.DisplayAuthor
                : (sourceMod.Author != null ? new List<string> { sourceMod.Author } : null),
            Description = PickString(mainMod.Description, sourceMod.Description),
            Aliases = PickList(mainMod.Aliases, sourceMod.Aliases),
            Replaces = PickList(mainMod.Replaces, sourceMod.Replaces),
            ModUrl = PickString(mainMod.ModUrl, sourceMod.ModUrl),
            RepoName = mainMod.RepoName,
            Dependencies = PickList(mainMod.Dependencies, sourceMod.Dependencies),
            Version = PickString(mainMod.Version, sourceMod.Version),
            Error = mainMod.Error,
            AutoUpdate = mainMod.AutoUpdate,
            Download = PickString(mainMod.Download, sourceMod.DownloadUrl),
            Downloads = PickList(mainMod.Downloads, sourceMod.Updater?.Downloads),
            AuthorUrl = PickString(mainMod.AuthorUrl, sourceMod.ModUrl),
            SupportUrl = mainMod.SupportUrl,
            Categories = PickList(mainMod.Categories, sourceMod.Categories),
            TestedOn = PickString(mainMod.TestedOn, sourceMod.TestedOn?.TldVersion),
            Released = PickString(mainMod.Released, sourceMod.Released),
            Updated = PickString(mainMod.Updated, sourceMod.Updated),
            Images = PickList(mainMod.Images, sourceMod.Images),
            Status = mainMod.Status,
            PreviousAuthors = mainMod.PreviousAuthors is { Count: > 0 }
                ? mainMod.PreviousAuthors
                : (sourceMod.PreviousAuthors != null ? new List<string> { sourceMod.PreviousAuthors } : null),
            Source = mainMod.Source
        };
    }

    public async Task<List<ModSummary>> GetMergedModsAsync()
    {
        var mainMods = await GetModsAsync();

        var tasks = mainMods.Select(async mod =>
        {
            var sourceMod = await GetSourceModAsync(mod.Source, mod.Name ?? mod.DisplayName ?? "");
            return MergeModData(mod, sourceMod);
        });

        return (await Task.WhenAll(tasks)).ToList();
    }
    
    public async Task DownloadAndInstallMelonLoaderAsync(string gameDirectory, IProgress<double>? progress = null)
    {
        const string melonLoaderUrl = "https://github.com/LavaGang/MelonLoader/releases/download/v0.7.2/MelonLoader.x64.zip";
        string zipPath = Path.Combine(Path.GetTempPath(), "MelonLoader.x64.zip");

        using (var client = new HttpClient())
        using (var response = await client.GetAsync(melonLoaderUrl, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(zipPath);
        
            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;
                if (progress != null && totalBytes > 0)
                    progress.Report((double)totalRead / totalBytes * 100);
            }
        }
        
        ZipFile.ExtractToDirectory(zipPath, gameDirectory, overwriteFiles: true);
        File.Delete(zipPath);
    }
}