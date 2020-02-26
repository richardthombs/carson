using System;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;

public class TwilightService
{
	HttpClient http;

	public TwilightService()
	{
		this.http = new HttpClient();
	}

	public async Task<TwilightInfo> Get()
	{
		var json = await http.GetStringAsync("https://api.sunrise-sunset.org/json?lat=52.8720114&lng=-0.8555282&formatted=0");
		var response = JsonConvert.DeserializeObject<SunriseSunsetResponse>(json);

		return new TwilightInfo
		{
			Sunrise = response.results.sunrise,
			Sunset = response.results.sunset,
			TwilightStart = response.results.civil_twilight_begin,
			TwilightEnd = response.results.civil_twilight_end
		};
	}
}

class SunriseSunsetResponse
{
	public SunriseSunsetResult results;
	public string status;
}

class SunriseSunsetResult
{
	public DateTimeOffset sunrise { get; set; }
	public DateTimeOffset sunset;
	public DateTimeOffset civil_twilight_begin;
	public DateTimeOffset civil_twilight_end;
}
