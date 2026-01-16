using System;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using OmniShell.Services;

namespace OmniShell.Views.Widgets;

public partial class WeatherWidget : System.Windows.Controls.UserControl
{
    private readonly HttpClient _httpClient;
    private readonly DispatcherTimer _refreshTimer;
    private bool _isLoading;
    
    public string? CurrentTemperature { get; private set; }
    public event EventHandler? WeatherUpdated;

    public WeatherWidget()
    {
        InitializeComponent();
        
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(30)
        };
        _refreshTimer.Tick += (s, e) => LoadWeather();
        _refreshTimer.Start();
        
        // Subscribe to location changes
        WidgetSettings.WeatherLocationChanged += (s, e) => LoadWeather();
        
        Loaded += async (s, e) => await LoadWeather();
        Unloaded += (s, e) => _refreshTimer.Stop();
    }

    private async System.Threading.Tasks.Task LoadWeather()
    {
        // Prevent concurrent loads
        if (_isLoading) return;
        _isLoading = true;
        
        try
        {
            // Show loading state
            Dispatcher.Invoke(() =>
            {
                ConditionText.Text = "Loading...";
            });
            
            // Get custom location or use auto-detect (empty string)
            var customLocation = WidgetSettings.GetWeatherLocation();
            var locationParam = string.IsNullOrWhiteSpace(customLocation) ? "" : customLocation;
            
            // Using wttr.in - free, no API key needed
            var url = $"https://wttr.in/{Uri.EscapeDataString(locationParam)}?format=j1";
            var response = await _httpClient.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            var root = json.RootElement;
            
            // Current conditions
            var current = root.GetProperty("current_condition")[0];
            var temp = current.GetProperty("temp_C").GetString();
            var feelsLike = current.GetProperty("FeelsLikeC").GetString();
            var humidity = current.GetProperty("humidity").GetString();
            var windSpeed = current.GetProperty("windspeedKmph").GetString();
            var weatherCode = current.GetProperty("weatherCode").GetString();
            var weatherDesc = current.GetProperty("weatherDesc")[0].GetProperty("value").GetString();
            
            // Location
            var nearestArea = root.GetProperty("nearest_area")[0];
            var city = nearestArea.GetProperty("areaName")[0].GetProperty("value").GetString();
            
            // Update UI on main thread
            Dispatcher.Invoke(() =>
            {
                TemperatureText.Text = temp;
                ConditionText.Text = weatherDesc ?? "Clear";
                FeelsLikeText.Text = $"Feels like {feelsLike}°C";
                HumidityText.Text = $"{humidity}%";
                WindText.Text = $"{windSpeed} km/h";
                LocationText.Text = city ?? locationParam;
                WeatherIcon.Text = GetWeatherEmoji(weatherCode ?? "113");
                
                // Update property and notify
                CurrentTemperature = temp;
                WeatherUpdated?.Invoke(this, EventArgs.Empty);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WeatherWidget] Error: {ex.Message}");
            Dispatcher.Invoke(() =>
            {
                // Only show error if no data was previously loaded
                if (string.IsNullOrEmpty(TemperatureText.Text) || TemperatureText.Text == "--")
                {
                    ConditionText.Text = "Unable to load";
                    LocationText.Text = "Check connection";
                }
            });
        }
        finally
        {
            _isLoading = false;
        }
    }

    private string GetWeatherEmoji(string code)
    {
        // Returns Segoe MDL2 Assets unicode characters
        return code switch
        {
            "113" => "\uE706",    // Clear/Sunny - Brightness
            "116" => "\uE9BF",    // Partly cloudy
            "119" or "122" => "\uE753",  // Cloudy/Overcast
            "143" or "248" or "260" => "\uE753",  // Fog/Mist
            "176" or "263" or "266" or "293" or "296" or "299" or "302" or "305" or "308" or "353" or "356" or "359" => "\uE754",  // Rain
            "179" or "182" or "185" or "281" or "284" or "311" or "314" or "317" or "320" or "350" or "362" or "365" or "374" or "377" => "\uE754",  // Sleet/Freezing
            "200" or "386" or "389" or "392" or "395" => "\uE754",  // Thunder
            "227" or "230" or "323" or "326" or "329" or "332" or "335" or "338" or "368" or "371" => "\uE9C8",  // Snow
            _ => "\uE706"  // Default sunny
        };
    }
}
