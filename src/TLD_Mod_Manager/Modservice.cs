using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TLD_Mod_Manager;

public class ModService
{
    private readonly HttpClient _httpClient = new();
    private const string ApiUrl = "https://tldmods.com/api.php";

    public async Task<List<Mod>> GetModsAsync()
    {
        try
        {
            string json = await _httpClient.GetStringAsync(ApiUrl);
            var apiMods = JsonSerializer.Deserialize<List<ApiMod>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiMods == null)
                return new List<Mod>();

            var result = apiMods.Select(apiMod => new Mod
            {
                Name = apiMod.DisplayName ?? apiMod.Name ?? "Unknown",
                Author = apiMod.DisplayAuthor?.FirstOrDefault() ?? apiMod.Author ?? "Unknown",
                Version = apiMod.Version ?? "",
                Description = apiMod.Description ?? "",
                Categories = apiMod.Categories ?? new List<string>(),
                Status = "Unknown",
                ImageUrl = apiMod.Images != null && apiMod.Images.Count > 0 ? apiMod.Images[0] : "",
                DownloadUrl = !string.IsNullOrEmpty(apiMod.Download) ? apiMod.Download :
                               (apiMod.Downloads != null && apiMod.Downloads.Count > 0 ? apiMod.Downloads[0] : ""),
                Dependencies = apiMod.Dependencies ?? new List<string>(),
                SourceUrl = apiMod.Source
            }).ToList();

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching mods from API: {ex.Message}");
            return new List<Mod>();
        }
    }

    public async Task<ModDetails?> GetModDetailsAsync(string sourceUrl)
    {
        try
        {
            string json = await _httpClient.GetStringAsync(sourceUrl);
            
            var modList = JsonSerializer.Deserialize<List<JsonModData>>(json);
            if (modList != null && modList.Any())
            {
                var fullData = modList.First();
                return CreateModDetails(fullData);
            }
            
            var container = JsonSerializer.Deserialize<ModListContainer>(json);
            if (container?.Mods != null && container.Mods.Any())
            {
                var fullData = container.Mods.First();
                return CreateModDetails(fullData);
            }
            
            var singleMod = JsonSerializer.Deserialize<JsonModData>(json);
            if (singleMod != null)
            {
                return CreateModDetails(singleMod);
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching mod details from {sourceUrl}: {ex.Message}");
            return null;
        }
    }

    private ModDetails CreateModDetails(JsonModData data)
    {
        return new ModDetails
        {
            Status = data.Status?.Working == true ? "WORKING" : (data.Status?.Working == false ? "NOT WORKING" : "Unknown"),
            TestedOn = data.TestedOn != null
                ? $"TLD {data.TestedOn.TldVersion} / ML {data.TestedOn.MlVersion}"
                : "Unknown",
            Beta = data.Status?.Beta ?? false,
            PatchNotes = data.Status?.PatchNotes ?? ""
        };
    }
}

public class ModListContainer
{
    [JsonPropertyName("mods")]
    public List<JsonModData>? Mods { get; set; }
}

public class ApiMod
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

    [JsonPropertyName("ModUrl")]
    public string? ModUrl { get; set; }

    [JsonPropertyName("Dependencies")]
    public List<string>? Dependencies { get; set; }

    [JsonPropertyName("Version")]
    public string? Version { get; set; }

    [JsonPropertyName("Download")]
    public string? Download { get; set; }

    [JsonPropertyName("Downloads")]
    public List<string>? Downloads { get; set; }

    [JsonPropertyName("Images")]
    public List<string>? Images { get; set; }

    [JsonPropertyName("Categories")]
    public List<string>? Categories { get; set; }

    [JsonPropertyName("Source")]
    public string? Source { get; set; }
}

public class JsonModData
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public List<string>? Categories { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? DownloadUrl { get; set; }
    public List<string>? Dependencies { get; set; }
    public ModStatus? Status { get; set; }
    public ModTestedOn? TestedOn { get; set; }
    public string? LastUpdated { get; set; }
    public bool TftftDlcRequired { get; set; }
    public bool IsLibrary { get; set; }
    public bool IsPlugin { get; set; }
}

public class ModStatus
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

public class ModTestedOn
{
    [JsonPropertyName("tldversion")]
    public string? TldVersion { get; set; }

    [JsonPropertyName("mlversion")]
    public string? MlVersion { get; set; }
}

public class ModDetails
{
    public string Status { get; set; } = "Unknown";
    public string TestedOn { get; set; } = "Unknown";
    public bool Beta { get; set; }
    public string PatchNotes { get; set; } = "";
}