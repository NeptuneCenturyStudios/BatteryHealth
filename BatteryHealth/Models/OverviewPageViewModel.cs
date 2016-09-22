using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;

namespace BatteryHealth.Models
{
    class OverviewPageViewModel : BaseModel
    {

        #region Properties

        public ObservableCollection<Battery> BatteryDevices = new ObservableCollection<Battery>();

        #endregion

        #region Methods

        /// <summary>
        /// Gets the aggregate battery info
        /// </summary>
        /// <returns></returns>
        public BatteryReport GetAggregateBattery()
        {
            // Get battery info
            var battery = Battery.AggregateBattery;
            if (battery != null)
            {
                return battery.GetReport();
            }

            return null;
        }

        /// <summary>
        /// Enumerates the battery devices
        /// </summary>
        public async Task EnumerateBatteries()
        {
            // Find batteries 
            var deviceInfo = await DeviceInformation.FindAllAsync(Battery.GetDeviceSelector());
            foreach (var device in deviceInfo)
            {
                try
                {
                    // Create battery object
                    var battery = await Battery.FromIdAsync(device.Id);
                    
                    // Get report
                    var report = battery.GetReport();

                    // Update UI
                    BatteryDevices.Add(battery);
                }
                catch { /* Add error handling, as applicable */ }
            }

        }

        #endregion
    }
}
