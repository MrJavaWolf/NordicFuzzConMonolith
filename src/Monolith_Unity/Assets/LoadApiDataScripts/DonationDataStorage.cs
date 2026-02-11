using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Monolith.DonationPolling.PollDonations
{
    public class DonationDataStorage
    {
        public static T Load<T>(string file) where T : class
        {

            if (!File.Exists(file))
            {
                return null;
            }

            try
            {
                string content = File.ReadAllText(file, Encoding.UTF8);
                var dto = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);

                if (dto == null)
                {
                    return null;
                }

                return dto;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load file: {file}: {ex}");
                return null;
            }
        }
    }
}