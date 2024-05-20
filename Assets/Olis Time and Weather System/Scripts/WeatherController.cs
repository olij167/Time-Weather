using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
//using UnityEditor;
namespace TimeWeather
{
#if UNITY_EDITOR
    [RequireComponent(typeof(WeatherDisplay))]
#endif
    public class WeatherController : MonoBehaviour
    {
        public static WeatherController instance;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        [System.Serializable]
        public class WeatherData
        {
            [Tooltip("The name of the weather condition used for identification. \n Ensure each condition has a unique name to avoid errors")]
            public string weatherCondition;
            [Header("Requirements for Weather Selection")]
            [Tooltip("The temperature range required for this condition. \n This can be used to differentiate between weather conditions")]
            public Vector2 tempRange;
            [Tooltip("The chance of rain range required for this condition. This can be used to differentiate between weather conditions")]
            public Vector2 rainRange;
            [Tooltip("Whether this condition requires it to be raining \n This can be used to differentiate between weather conditions")]
            public bool isRaining;

            [Space(10)]
            [Header("Weather Condition Effects")]
            [Tooltip("The strength range of the clouds when this condition is active \n 0 = full cloud coverage, 5 = no clouds")]
            public Vector2 cloudPowerRange;
            [Tooltip("The strength of the fog when this condition is active")]
            [Range(0f, 1f)] public float fogStrength;
            //[Tooltip("The colour of the fog when this condition is active, depending on the time of day")]
            //public Gradient fogColour;
            [Tooltip("The level of surface wetness for materials with the 'Wet' or 'WetAndSnowy' shader during this condition")]
            public float wetness;
            [Tooltip("The level of surface snowiness for materials with the 'Snowy' or 'WetAndSnowy' shader during this condition")]
            public float snowiness;
            [Tooltip("The particles which are active during this condition")]
            public WeatherParticles[] weatherParticles;
            [Tooltip("an array of ambient audio clips to play during this condition, picked randomly")]
            public AudioClip[] clips;
            [Tooltip("The pitch to play the clip at")]
            public float pitch;
            [Tooltip("The volume to play the clip at")]
            public float volume;
        }

        [System.Serializable]
        public class WeatherParticles
        {
            [Tooltip("The particle system to play")]
            public ParticleSystem particleSystem;
            public ParticleSystem.EmissionModule particleEmission;
            [Tooltip("The number of particles to spawn - the particle system's emission rate")]
            [Range(0f, 5000f)] public float particleAmount;
            public ParticleSystem.NoiseModule particleNoise;
            [Tooltip("the strength of noise applied to the particles direction - the particle system's noise strength")]
            [Range(0f, 10f)] public float noiseStrength;
        }

        [HideInInspector] public TimeController timeController;

        [Header("Current Weather")]
        #region
        [Tooltip("The weather preset that is currently selected")]
        [field: ReadOnlyField] public WeatherData currentWeatherPreset;
        private string weatherCondition;
        [field: ReadOnlyField] public float rainRandomiser;

        [Tooltip("The current temperature")]
        public float temperature;
        [Tooltip("The current chance of rain")]
        [Range(0f, 100f)] public float rainChance;

        [Tooltip("The current cloud power \n 0 = full cloud coverage, 5 = clear sky")]
        [Range(0f, 5f)] public float cloudPower;
        [Tooltip("The speed which fog changes based on the current weather preset")]
        [Range(0f, 1f)] public float fogSpeed;
        [Space(5f)]

        [Tooltip("The current surface wetness. \n Only applied to materials with the 'Wet' or 'WetAndSnowy' shader")]
        [Range(0f, 5f)] public float wetness;
        [SerializeField, Range(0f, 5f)] private float desiredWetness;
        [Tooltip("The current surface snowiness. \n Only applied to materials with the 'Snowy' or 'WetAndSnowy' shader")]
        [Range(0f, 1f)] public float snowiness;
        [SerializeField, Range(0f, 1f)] private float desiredSnowiness;
        [Space(5f)]

        [Header("Surface Condition Variables")]
        [Tooltip("The speed which surface wetness increases based on the current weather preset")]
        [Range(0f, 1f)] public float wetnessSpeed;
        [Tooltip("The speed which surface wetness decreases based on the current weather preset")]
        [Range(0f, 1f)] public float evaporationSpeed;
        [Space(5f)]

        [Tooltip("The speed which surface snowiness increases based on the current weather preset")]
        [Range(0f, 1f)] public float snowCoverSpeed;
        [Tooltip("The speed which surface snowiness decreases based on the current weather preset")]
        [Range(0f, 1f)] public float meltingSpeed;
        #endregion
        //[Space(10)]
        [Tooltip("The season that is currently selected")]
        [ReadOnlyField] public SeasonConditions currentSeasonConditions;

        [Header("Hourly Forcast")]
        [Tooltip("The hourly forcast for the day. It is generated at midnight and on start based on the currentSeasonConditions")]
        [field: ReadOnlyField] public List<HourlyWeather> hourlyWeather;

        [Header("Current Wind")]
        [Tooltip("controls the speed of the clouds")]
        public float windMultiplier = 1e-08f; // you should keep this low
        [Tooltip("The current wind direction controlling the clouds movement")]
        [field: ReadOnlyField] public Vector2 wind = new Vector2();
        [Tooltip("The speed of the wind")]
        [field: ReadOnlyField] public float windSpeed;

        [Header("Weather Presets")]
        [Tooltip("The list of seasons and their conditions. \n At least 1 season is required to function")]
        public List<SeasonConditions> seasonConditions;

        [Tooltip("An array of all potential weather conditions, their requirements and their effects")]
        public WeatherData[] weatherDataPresets;
        [Tooltip("The renderer attached to the cloud plane")]
        public Renderer[] cloudRenderer;
        [Tooltip("A CrossFadeAudio component, \n this plays weather audio clips and allows for smoother audio transitions between clips")]
        public CrossFadeAudio weatherAudio;

        public ParticleSystem.EmissionModule particleEmission;
        public ParticleSystem.NoiseModule particleNoise;


        [Header("UI")]
        [Tooltip("Toggle whether to show the temperature UI")]
        public bool toggleTempUI;
        [Tooltip("The TextMeshPro element to display the current temperature")]
        public TextMeshProUGUI tempText;
        [Tooltip("Toggle whether to show the rain chance UI")]
        public bool toggleRainUI;
        [Tooltip("The TextMeshPro element to display the current chance of rain")]
        public TextMeshProUGUI rainText;
        [Tooltip("Toggle whether to show the weather condition UI")]
        public bool toggleWeatherUI;
        [Tooltip("The TextMeshPro element to display the current weather condition")]
        public TextMeshProUGUI weatherConditionText;

        public bool showDebugLogs;

        private void OnEnable()
        {
            timeController = TimeController.instance;
        }


        private void Start()
        {
            timeController = TimeController.instance;

            SetSeasonalConditions();
            SetDailyConditions();


            foreach (WeatherData data in weatherDataPresets)
            {
                for (int w = 0; w < data.weatherParticles.Length; w++)
                {
                    if (data.weatherParticles[w].particleSystem != null && data.weatherParticles[w].particleSystem.isPlaying)
                    {
                        data.weatherParticles[w].particleSystem.Stop();
                    }
                }
            }

            rainChance = hourlyWeather[timeController.timeHours].rainChance;
            wetness = currentWeatherPreset.wetness;
            snowiness = currentWeatherPreset.snowiness;

            ToggleUI();


        }

        public void ToggleUI()
        {
            if (!toggleTempUI)
            {
                if (tempText != null) tempText.gameObject.SetActive(false);
            }
            else if (tempText != null) tempText.gameObject.SetActive(true);

            if (!toggleRainUI)
            {
                if (rainText != null) rainText.gameObject.SetActive(false);
            }
            else if (rainText != null) rainText.gameObject.SetActive(true);

            if (!toggleWeatherUI)
            {
                if (weatherConditionText != null) weatherConditionText.gameObject.SetActive(false);
            }
            else if (weatherConditionText != null) weatherConditionText.gameObject.SetActive(true);
        }

        public void SetSeasonalConditions()
        {
            for (int i = 0; i < seasonConditions.Count; i++)
            {
                if (timeController.currentMonthData.season == seasonConditions[i].season)
                {
                    currentSeasonConditions = seasonConditions[i];
                    timeController.skyData = seasonConditions[i].seasonalSkyData;
                    if (showDebugLogs) Debug.Log("It is now " + seasonConditions[i].season);
                    return;
                }
            }
        }

        public void SetDailyConditions()
        {
            HourlyWeather midnightCondition = new HourlyWeather();

            if (hourlyWeather.Count > 24)
            {
                midnightCondition = hourlyWeather[23];
                midnightCondition.isMidnight = true;
                //Debug.Log("Midnight condition set");
            }

            float highTemp = Random.Range(currentSeasonConditions.tempRange.x + Mathf.Abs(currentSeasonConditions.tempRange.x * 0.5f), currentSeasonConditions.tempRange.y);
            float lowTemp = Random.Range(currentSeasonConditions.tempRange.x, currentSeasonConditions.tempRange.y - Mathf.Abs(currentSeasonConditions.tempRange.y * 0.5f));

            int hottestTime = Random.Range((int)currentSeasonConditions.hottestTimeRange.x, (int)currentSeasonConditions.hottestTimeRange.y);
            int coldestMorningTime = Random.Range((int)currentSeasonConditions.coldestMorningTimeRange.x, (int)currentSeasonConditions.coldestMorningTimeRange.y);
            int coldestNightTime = Random.Range((int)currentSeasonConditions.coldestNightTimeRange.x, (int)currentSeasonConditions.coldestNightTimeRange.y);

            timeController.sunriseTime = Random.Range(currentSeasonConditions.sunriseTimeRange.x, currentSeasonConditions.sunriseTimeRange.y);
            timeController.sunsetTime = Random.Range(currentSeasonConditions.sunsetTimeRange.x, currentSeasonConditions.sunsetTimeRange.y);

            timeController.sunriseTimePercent = (timeController.sunriseTime %= 24) / 24f;
            timeController.sunsetTimePercent = (timeController.sunsetTime %= 24) / 24f;

            int sunriseHour = (int)timeController.sunriseTime;
            float sunriseTimeClamped = Mathf.Clamp((timeController.sunriseTime - sunriseHour) * 60, 0f, 59.49f);

              int sunsetHour = (int)timeController.sunsetTime;
            float sunsetTimeClamped = Mathf.Clamp((timeController.sunsetTime - sunsetHour) * 60, 0f, 59.49f);

            if (timeController.toggleSunTimeUI)
            {
                timeController.sunriseText.text = sunriseHour.ToString("00") + ":" + sunriseTimeClamped.ToString("00") + " AM \n Sunrise";
                timeController.sunsetText.text = sunsetHour.ToString("00") + ":" + sunsetTimeClamped.ToString("00") + " PM \n Sunset";
            }

            float lowestRainChance = Random.Range((int)currentSeasonConditions.rainRange.x, (int)currentSeasonConditions.rainRange.y);
            float highestRainChance = Random.Range((int)lowestRainChance, (int)currentSeasonConditions.rainRange.y);
            int heighestRainTime = Random.Range(0, 23);

            if (showDebugLogs)
            {
                Debug.Log("Highest rain chance = " + highestRainChance + ", lowest rain chance = " + lowestRainChance);
                Debug.Log("Highest temp = " + highTemp + ", lowest temp = " + lowTemp);
            }

            hourlyWeather = new List<HourlyWeather>(24);

            for (int i = 0; i < 24; i++)
            {
                HourlyWeather hourly = new HourlyWeather();

                if (midnightCondition.isMidnight && i == 0)
                {
                    hourly.forcastTime = i;
                    hourly.temp = midnightCondition.temp;
                    hourly.rainChance = midnightCondition.rainChance;
                    hourly.weatherCondition = midnightCondition.weatherCondition;
                    hourly.cloudPower = midnightCondition.cloudPower;
                    hourly.windSpeed = midnightCondition.windSpeed;
                    hourly.isRaining = midnightCondition.isRaining;
                    hourly.isMidnight = false;

                }
                else
                {
                    hourly.forcastTime = i;

                    if (i == coldestMorningTime) //set the temp for the coldest time in the morning
                    {
                        hourly.temp = lowTemp;
                    }
                    else if (i == hottestTime) //set the temp for the hottest time of the day
                    {
                        hourly.temp = highTemp;
                    }
                    else if (i == coldestNightTime) //set the temp for the coldest time in the night
                    {
                        hourly.temp = lowTemp;
                    }
                    else // lerp unspecifed temps between the highs and lows
                    {
                        if (i <= coldestMorningTime)
                        {
                            hourly.temp = coldestMorningTime;
                        }
                        else if (i > coldestMorningTime && i <= hottestTime)
                        {
                            hourly.temp = Mathf.Lerp(hourlyWeather[i - 1].temp, highTemp, 1f / (hottestTime - hourly.forcastTime));
                        }
                        else if (i > hottestTime && i < coldestNightTime)
                        {
                            hourly.temp = Mathf.Lerp(hourlyWeather[i - 1].temp, lowTemp, 1f / (coldestNightTime - hourly.forcastTime));
                        }
                        else if (i >= coldestNightTime)
                        {
                            hourly.temp = coldestNightTime;
                        }
                    }

                    // Set chance of rain for each hour
                    if (i == heighestRainTime) //Set highest chance of rain
                    {
                        hourly.rainChance = highestRainChance;

                    }
                    else if (i < heighestRainTime) // lerp the rest based on the highest time
                    {
                        if (i > 0)
                        {
                            hourly.rainChance = Mathf.Lerp(hourlyWeather[i - 1].rainChance, highestRainChance, 1f / (heighestRainTime - hourly.forcastTime));
                        }
                        else
                        {
                            hourly.rainChance = Mathf.Lerp(rainChance, highestRainChance / 2, 1f / (heighestRainTime - hourly.forcastTime));
                        }
                    }
                    else if (i > heighestRainTime)
                    {
                        hourly.rainChance = Mathf.Lerp(rainChance, Random.Range(lowestRainChance, hourlyWeather[i - 1].rainChance), 1f / (hourly.forcastTime - heighestRainTime));
                    }

                    rainRandomiser = Random.Range(0, 100);

                    if (showDebugLogs) Debug.Log(hourly.forcastTime + "o'clock Rain Chance = " + rainRandomiser);

                    //Set Weather Conditions
                    if (rainRandomiser < hourly.rainChance) // check whether it is raining
                    {
                        hourly.isRaining = true;
                    }

                    //Check required temp and chance of rain rnages, and whether it is raining
                    //Select the most appropriate weather

                    for (int w = 0; w < weatherDataPresets.Length; w++)
                    {
                        if (hourly.temp >= weatherDataPresets[w].tempRange.x && hourly.temp <= weatherDataPresets[w].tempRange.y)
                        {
                            if (hourly.rainChance >= weatherDataPresets[w].rainRange.x && hourly.rainChance <= weatherDataPresets[w].rainRange.y)
                            {
                                if (hourly.isRaining == weatherDataPresets[w].isRaining)
                                {
                                    hourly.weatherCondition = weatherDataPresets[w].weatherCondition;
                                    hourly.cloudPower = Random.Range(weatherDataPresets[w].cloudPowerRange.x, weatherDataPresets[w].cloudPowerRange.y);

                                    if (weatherDataPresets[w].clips != null)
                                    {
                                        hourly.weatherAudio = weatherDataPresets[w].clips[Random.Range(0, weatherDataPresets[w].clips.Length)];
                                    }

                                    //if (i > 0 && hourlyWeather[i - 1].isRaining)
                                    //{
                                    //    if (hourlyWeather[i - 1].wetness > weatherDataPresets[w].wetness)
                                    //        hourly.wetness = hourlyWeather[i - 1].wetness;

                                    //    if (hourlyWeather[i - 1].snowiness > weatherDataPresets[w].snowiness)
                                    //        hourly.snowiness = hourlyWeather[i - 1].snowiness;
                                    //}
                                    //else
                                    //{
                                    //    hourly.wetness = weatherDataPresets[w].wetness;
                                    //    hourly.snowiness = weatherDataPresets[w].snowiness;
                                    //}

                                    if (i > 0)
                                    {
                                        hourly.windSpeed = Random.Range(hourlyWeather[i - 1].windSpeed - 10f, hourlyWeather[i - 1].windSpeed + 10f);
                                    }
                                    else
                                        hourly.windSpeed = Random.Range(windSpeed - 10f, windSpeed + 10f);

                                    break;
                                }
                            }
                        }
                    }
                }

                hourlyWeather.Add(hourly);
            }

            temperature = hourlyWeather[timeController.timeHours].temp;

            rainChance = hourlyWeather[timeController.timeHours].rainChance;

            weatherCondition = hourlyWeather[timeController.timeHours].weatherCondition;
        }

        public void SetHourlyVariables(int hour, float temperature, float rainChance, bool isRaining)
        {
            hourlyWeather[hour].temp = temperature;
            hourlyWeather[hour].rainChance = rainChance;
            hourlyWeather[hour].isRaining = isRaining;

            for (int w = 0; w < weatherDataPresets.Length; w++)
            {
                if (hourlyWeather[hour].temp >= weatherDataPresets[w].tempRange.x && hourlyWeather[hour].temp <= weatherDataPresets[w].tempRange.y)
                {
                    if (hourlyWeather[hour].rainChance >= weatherDataPresets[w].rainRange.x && hourlyWeather[hour].rainChance <= weatherDataPresets[w].rainRange.y)
                    {
                        if (hourlyWeather[hour].isRaining == weatherDataPresets[w].isRaining)
                        {
                            hourlyWeather[hour].weatherCondition = weatherDataPresets[w].weatherCondition;
                            hourlyWeather[hour].cloudPower = Random.Range(weatherDataPresets[w].cloudPowerRange.x, weatherDataPresets[w].cloudPowerRange.y);

                            if (weatherDataPresets[w].clips != null)
                            {
                                hourlyWeather[hour].weatherAudio = weatherDataPresets[w].clips[Random.Range(0, weatherDataPresets[w].clips.Length)];
                            }

                            //hourlyWeather[hour].wetness = weatherDataPresets[w].wetness;
                            //hourlyWeather[hour].snowiness = weatherDataPresets[w].snowiness;

                            break;
                        }
                    }
                }
            }
        }

        public void SetCurrentConditions()
        {
            if (hourlyWeather != null)
            {
                if (timeController.timeHours < 23)
                {
                    float hourlyTimePercent = timeController.hourlyTimePercent;

                    temperature = Mathf.Lerp(hourlyWeather[timeController.timeHours].temp, hourlyWeather[timeController.timeHours + 1].temp, hourlyTimePercent);

                    if (toggleTempUI && tempText != null)
                        tempText.text = temperature.ToString("00") + "�C";

                    //weatherCondition = hourlyWeather[timeController.timeHours].weatherCondition;
                    for (int w = 0; w < weatherDataPresets.Length; w++)
                    {
                        if (temperature >= weatherDataPresets[w].tempRange.x && temperature <= weatherDataPresets[w].tempRange.y)
                        {
                            if (rainChance >= weatherDataPresets[w].rainRange.x && rainChance <= weatherDataPresets[w].rainRange.y)
                            {
                                if (hourlyWeather[timeController.timeHours].isRaining == weatherDataPresets[w].isRaining)
                                {
                                    weatherCondition = weatherDataPresets[w].weatherCondition;
                                    if (weatherConditionText != null)
                                        weatherConditionText.text = hourlyWeather[timeController.timeHours].weatherCondition;
                                    break;
                                }
                            }
                        }
                    }
                    if (toggleWeatherUI && weatherConditionText != null)
                        weatherConditionText.text = weatherCondition;

                    rainChance = Mathf.Lerp(hourlyWeather[timeController.timeHours].rainChance, hourlyWeather[timeController.timeHours + 1].rainChance, hourlyTimePercent);
                   
                    if (toggleRainUI && rainText != null)
                        rainText.text = rainChance.ToString("00") + "% Rain";


                    cloudPower = Mathf.Lerp(hourlyWeather[timeController.timeHours].cloudPower, hourlyWeather[timeController.timeHours + 1].cloudPower, hourlyTimePercent);
                    for (int i = 0; i < cloudRenderer.Length; i++)
                    {
                        cloudRenderer[i].material.SetFloat("_CloudPower", cloudPower);

                        windSpeed = Mathf.Lerp(hourlyWeather[timeController.timeHours].windSpeed, hourlyWeather[timeController.timeHours + 1].windSpeed, hourlyTimePercent);

                        wind.x = Mathf.Lerp(wind.x, windSpeed * windMultiplier, hourlyTimePercent);
                        wind.y = wind.x;
                        cloudRenderer[i].material.SetVector("_CloudSpeed", wind * timeController.timeScale * ((i + 1) * 25));
                    }


                    if (currentWeatherPreset.snowiness < snowiness)
                    {
                        desiredSnowiness = Mathf.Lerp(snowiness, currentWeatherPreset.snowiness, hourlyTimePercent * evaporationSpeed);
                    }
                    else if (currentWeatherPreset.snowiness > snowiness)
                    {
                        desiredSnowiness = Mathf.Lerp(snowiness, currentWeatherPreset.snowiness, hourlyTimePercent * snowCoverSpeed);
                    }

                    if (snowiness != desiredSnowiness)
                        snowiness = Mathf.Lerp(snowiness, desiredSnowiness, hourlyTimePercent * 0.5f);

                    Shader.SetGlobalFloat("_SnowAmount", snowiness);

                    if (currentWeatherPreset.wetness < wetness)
                    {
                        desiredWetness = Mathf.Lerp(wetness, currentWeatherPreset.wetness, hourlyTimePercent * evaporationSpeed);
                    }
                    else if (currentWeatherPreset.wetness > wetness)
                    {
                        desiredWetness = Mathf.Lerp(wetness, currentWeatherPreset.wetness, hourlyTimePercent * wetnessSpeed);
                    }

                    if (wetness != desiredWetness)
                        wetness = Mathf.Lerp(wetness, desiredWetness, hourlyTimePercent * 0.5f);

                    Shader.SetGlobalFloat("_Wetness", wetness);


                    for (int i = 0; i < weatherDataPresets.Length; i++)
                    {
                        if (weatherDataPresets[i].weatherCondition == weatherCondition)
                        {
                            currentWeatherPreset = weatherDataPresets[i];
                            //Debug.Log("Current weather preset: " + currentWeatherPreset.weatherCondition);

                            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, currentWeatherPreset.fogStrength, hourlyTimePercent * fogSpeed);
                            //RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, currentWeatherPreset.fogColour, hourlyTimePercent * fogSpeed);


                            if (currentWeatherPreset.clips != null)
                            {
                                weatherAudio.newSoundtrack(hourlyWeather[timeController.timeHours].weatherAudio, currentWeatherPreset.volume);

                            }

                        }

                        if (currentWeatherPreset.weatherParticles.Length > 0)
                        {
                            {
                                for (int w = 0; w < currentWeatherPreset.weatherParticles.Length; w++)
                                {
                                    if (currentWeatherPreset.weatherParticles[w].particleSystem != null)
                                    {
                                        particleEmission = currentWeatherPreset.weatherParticles[w].particleSystem.emission;
                                        particleNoise = currentWeatherPreset.weatherParticles[w].particleSystem.noise;

                                        particleEmission.rateOverTime = Mathf.Lerp(particleEmission.rateOverTime.constant, currentWeatherPreset.weatherParticles[w].particleAmount, hourlyTimePercent);
                                        particleNoise.strength = Mathf.Lerp(particleNoise.strength.constant, currentWeatherPreset.weatherParticles[w].noiseStrength, hourlyTimePercent);

                                        if (!currentWeatherPreset.weatherParticles[w].particleSystem.isPlaying)
                                            currentWeatherPreset.weatherParticles[w].particleSystem.Play();
                                    }
                                }

                            }
                        }

                        if (weatherDataPresets[i].weatherParticles.Length > 0)
                        {
                            List<ParticleSystem> presetParticles = new List<ParticleSystem>();
                            List<ParticleSystem> currentParticles = new List<ParticleSystem>();

                            for (int w = 0; w < weatherDataPresets[i].weatherParticles.Length; w++)
                            {
                                presetParticles.Add(weatherDataPresets[i].weatherParticles[w].particleSystem);

                                for (int cw = 0; cw < currentWeatherPreset.weatherParticles.Length; cw++)
                                {
                                    currentParticles.Add(currentWeatherPreset.weatherParticles[cw].particleSystem);
                                }

                                if (!currentParticles.Contains(presetParticles[w]))
                                {
                                    presetParticles[w].Stop();
                                }
                            }
                        }

                    }

                    if (temperature <= 0f && currentWeatherPreset.isRaining)
                    {
                        Shader.SetGlobalFloat("_isSnowing", 1);
                    }
                    else if (temperature > 0f && currentWeatherPreset.isRaining)
                    {
                        Shader.SetGlobalFloat("_isSnowing", 0);
                    }
                }
            }
        }


        private void Update()
        {
            SetCurrentConditions();
            ToggleUI();
        }
    }

    [System.Serializable]
    public class SeasonConditions
    {
        [Tooltip("The name of this season")]
        public string season;
        [Tooltip("The temperature range of this season. \n This can be used to control which weather will be more likely to occur")]
        public Vector2 tempRange;
        [Tooltip("The chance of rain range of this season. \n This can be used to control which weather will be more likely to occur")]
        public Vector2 rainRange;
        [Tooltip("The range of time which will be the hottest in the day")]
        public Vector2 hottestTimeRange;
        [Tooltip("The range of time which will be the coldest in the morning")]
        public Vector2 coldestMorningTimeRange;
        [Tooltip("The range of time which will be the coldest at night")]
        public Vector2 coldestNightTimeRange;
        [Tooltip("The range of time which the sun will rise")]
        public Vector2 sunriseTimeRange;
        [Tooltip("The range of time which the sun will set")]
        public Vector2 sunsetTimeRange;

        [Tooltip("Sky Data for the season. \n This allows for seasonal colour changes to the skybox and differences in the sun size and intensity")]
        public TimeController.SkyData seasonalSkyData;
    }

    [System.Serializable]
    public class HourlyWeather
    {
        [Tooltip("The time of this forcast hour")]
        public int forcastTime;
        [Tooltip("The temperature at the start of this forcast hour")]
        public float temp;
        [Tooltip("The chance of rain at the start of this forcast hour")]
        public float rainChance;
        [Tooltip("The weather condition of this forcast hour")]
        public string weatherCondition;
        [Tooltip("The cloud strength this forcast hour \n 0 = full cloud cover, 5 = clear sky")]
        public float cloudPower;
        [Tooltip("The windspeed this forcast hour")]
        public float windSpeed;
        //[Tooltip("The surface wetness of this forcast hour \n Only applied to materials with the 'Wet' or 'WetAndSnowy' shader")]
        //public float wetness;
        //[Tooltip("The surface snowiness of this forcast hour  \n Only applied to materials with the 'Snowy' or 'WetAndSnowy' shader")]
        //public float snowiness;
        [Tooltip("Whether it is raining during this hour")]
        public bool isRaining = false;
        [HideInInspector] public bool isMidnight = false;
        [Tooltip("The audio clip to play during this hours weather. \n selected randomly from 'clips' in the weather preset")]
        public AudioClip weatherAudio;
    }
}

