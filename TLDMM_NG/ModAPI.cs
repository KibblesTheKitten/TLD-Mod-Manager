using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TLDMM_NG;

public class ModApi
{
    private readonly HttpClient _httpClient = new HttpClient();
    private const string APIURL = "https://tldmods.net/api.php";

    public async Task<List<ModApi>> GetMods()
    {
        try
        {
            string json = await _httpClient.GetStringAsync(APIURL);
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
                SourceUrl = apiMod.Source,
                IsLibrary = apiMod.Type?.Equals("library", StringComparison.OrdinalIgnoreCase) ?? false,
                IsPlugin = apiMod.Type?.Equals("plugin", StringComparison.OrdinalIgnoreCase) ?? false
            }).ToList();

            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);Debug.WriteLine($"Error fetching mods from API: {ex.Message}");
            return new List<Mod>();
            throw;
        }
    }
}