using Monolith.DonationPolling.PollDonations;
using System.IO;
using UnityEngine;

public class DataStorage : MonoBehaviour
{

    private NordicFuzzConConfiguration _NordicFuzzConConfiguration = null;
    public NordicFuzzConConfiguration NordicFuzzConConfiguration
    {
        get
        {

            if (_NordicFuzzConConfiguration == null)
            {
                string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
                string configPath = Path.Combine(exeDirectory, "config.json");
                Debug.Log($"Loads config from path: {configPath}");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    _NordicFuzzConConfiguration = Newtonsoft.Json.JsonConvert.DeserializeObject<NordicFuzzConConfiguration>(json);
                    Debug.Log("Config loaded successfully.");
                }
                else
                {
                    Debug.LogError("Config file not found at: " + configPath);
                    _NordicFuzzConConfiguration = new NordicFuzzConConfiguration(); // fallback defaults
                }

            }
            return _NordicFuzzConConfiguration;
        }
    }


    private DonationDataPaths _DonationDataPaths = null;
    public DonationDataPaths DonationDataPaths
    {
        get
        {
            if (_DonationDataPaths == null)
            {
                _DonationDataPaths = new DonationDataPaths()
                {
                    Enable = true,
                    Path = NordicFuzzConConfiguration.DonationDataBasePath,
                };
            }
            return _DonationDataPaths;
        }
    }
}
