using Monolith.DonationPolling.PollDonations;
using UnityEngine;

public class DataStorage : MonoBehaviour
{
    public bool Enable = true;
    public string DataBasePath = string.Empty;

    private DonationDataPaths _DonationDataPaths = null;
    public DonationDataPaths DonationDataPaths
    {
        get
        {
            if (_DonationDataPaths == null)
            {
                _DonationDataPaths = new DonationDataPaths()
                {
                    Enable = Enable,
                    Path = DataBasePath,
                };
            }
            return _DonationDataPaths;
        }
    }
}
