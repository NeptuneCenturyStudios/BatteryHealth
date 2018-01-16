using BatteryHealth.DataModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace BatteryHealth.Models
{
    class OverviewPageViewModel : BaseModel
    {

        #region Fields
        private char[][] _batteryIndicators;
        private TaskFactory _uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());

        #endregion

        #region Properties

        /// <summary>
        /// Gets the battery devices on the device
        /// </summary>
        public ObservableCollection<Battery> BatteryDevices { get; set; } = new ObservableCollection<Battery>();

        /// <summary>
        /// Gets a collection of charge data
        /// </summary>
        public ObservableCollection<ChargeDetail> ChargeData { get; set; } = new ObservableCollection<ChargeDetail>();


        private int? _designCap;
        /// <summary>
        /// Gets the design capacity of the battery (mWh)
        /// </summary>
        public int? DesignCap
        {
            get { return _designCap; }
            protected set
            {
                _designCap = value;
                // Notify change
                OnPropertyChanged(nameof(DesignCap));
                OnPropertyChanged(nameof(PercentRemaining));
            }
        }

        /// <summary>
        /// Gets the max negative design capacity
        /// </summary>
        public int? NegativeDesignCap
        {
            get { return -DesignCap; }
        }

        /// <summary>
        /// Gets the chart interval based on design capacity
        /// </summary>
        public int? ChartInterval
        {
            get { return (DesignCap - NegativeDesignCap) / 10; }
        }

        private int? _fullChargeCap;
        /// <summary>
        /// Gets the full charge capacity of the battery (mWh)
        /// </summary>
        public int? FullChargeCap
        {
            get { return _fullChargeCap; }
            protected set
            {
                _fullChargeCap = value;
                // Notify change
                OnPropertyChanged(nameof(FullChargeCap));
                OnPropertyChanged(nameof(EffectivePercentRemaining));
                OnPropertyChanged(nameof(BatteryIndicator));
            }
        }


        private int? _remainingCap;
        /// <summary>
        /// Gets the remaining capacity
        /// </summary>
        public int? RemainingCap
        {
            get { return _remainingCap; }
            protected set
            {
                _remainingCap = value;
                // Notify change
                OnPropertyChanged(nameof(RemainingCap));
                OnPropertyChanged(nameof(PercentRemaining));
                OnPropertyChanged(nameof(EffectivePercentRemaining));
                OnPropertyChanged(nameof(BatteryIndicator));
            }
        }

        private int? _chargeRate;
        /// <summary>
        /// Gets the charge rate
        /// </summary>
        public int? ChargeRate
        {
            get { return _chargeRate; }
            protected set
            {
                _chargeRate = value;
                // Notify change
                OnPropertyChanged(nameof(ChargeRate));
            }
        }


        private Windows.System.Power.BatteryStatus _batteryStatus;
        /// <summary>
        /// Gets or sets the battery status
        /// </summary>
        private Windows.System.Power.BatteryStatus BatteryStatus
        {
            get { return _batteryStatus; }
            set
            {
                _batteryStatus = value;
                // Notify change
                OnPropertyChanged(nameof(FormattedStatus));
            }
        }

        /// <summary>
        /// Gets or sets the aggregate status
        /// </summary>
        public string FormattedStatus
        {
            get
            {
                string formattedStatus;
                // Determine status
                switch (BatteryStatus)
                {
                    case Windows.System.Power.BatteryStatus.Charging:
                        formattedStatus = "Charging";
                        break;

                    case Windows.System.Power.BatteryStatus.Discharging:
                        formattedStatus = "Discharging";
                        break;

                    case Windows.System.Power.BatteryStatus.Idle:
                        formattedStatus = "Not Charging";
                        break;

                    case Windows.System.Power.BatteryStatus.NotPresent:
                        formattedStatus = "Not Present";
                        break;

                    default:
                        formattedStatus = "Unknown";
                        break;
                }

                return formattedStatus;
            }
        }


        /// <summary>
        /// Gets the percentage remaining of the battery
        /// </summary>
        public decimal? PercentRemaining
        {
            get
            {
                // If the MilliwattHours properties are null, assume 100 percent
                var remaining = RemainingCap / (decimal?)DesignCap;
                // Check status. If the status is idle, then report 100%
                if (BatteryStatus == Windows.System.Power.BatteryStatus.Idle)
                {
                    remaining = 1;
                }

                return remaining;
            }
        }

        /// <summary>
        /// Gets the effective percentage remaining.
        /// </summary>
        public decimal? EffectivePercentRemaining
        {
            get
            {
                // If the MilliwattHours properties are null, assume 100 percent
                var remaining = RemainingCap / (decimal?)FullChargeCap;
                // Check status. If the status is idle, then report 100%
                if (BatteryStatus == Windows.System.Power.BatteryStatus.Idle)
                {
                    remaining = 1;
                }

                return remaining;
            }
        }

        /// <summary>
        /// Gets the indicator for the battery status
        /// </summary>
        public char BatteryIndicator
        {
            get
            {
                // Get the battery status
                var index = (int)Math.Round(EffectivePercentRemaining.GetValueOrDefault() * 100 / 10, 0);

                // If the status is idle, index is 0
                if (BatteryStatus == Windows.System.Power.BatteryStatus.Idle)
                {
                    index = 0;
                }

                // Get indicator
                var indicator = _batteryIndicators[(int)BatteryStatus][index];

                return indicator;

            }
        }


        /// <summary>
        /// Gets the overall efficiency of the battery
        /// </summary>
        public double? Efficiency
        {
            get
            {
                return FullChargeCap / (double?)DesignCap;
            }
        }

        /// <summary>
        /// Gets the efficiency status message
        /// </summary>
        public string EfficiencyStatus
        {
            get
            {
                if (Efficiency != null && Efficiency <= 0.25)
                {
                    return $"Your battery can only hold {Efficiency:P2} of its designed capacity. Please replace your battery.";
                }
                else if (Efficiency != null && Efficiency <= 0.50)
                {
                    return $"Your battery can only hold {Efficiency:P2} of its designed capacity. Consider replacing your battery.";
                }
                else
                {
                    return $"Your battery is holding {Efficiency:P2} of its designed capacity.";
                }
            }
        }

        /// <summary>
        /// Gets an indicator for the efficiency status
        /// </summary>
        public Indicator EfficiencyIndicator
        {
            get
            {
                // F
                if (Efficiency != null && Efficiency <= 0.25)
                {
                    // Critical #FFC70000
                    return new Indicator()
                    {
                        Symbol = (char)0xE996,
                        Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xC7, 0x00, 0x00)),
                        Label = "Critical"
                    };
                }
                else if (Efficiency != null && Efficiency <= 0.50)
                {
                    // Poor #FFC77F00
                    return new Indicator()
                    {
                        Symbol = (char)0xE730,
                        Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xC7, 0x7F, 0x00)),
                        Label = "Poor"
                    };
                }
                else if (Efficiency != null && Efficiency <= 0.75)
                {
                    // Fair #FF00C724
                    return new Indicator()
                    {
                        Symbol = (char)0xE8E1,
                        Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xC7, 0x24)),
                        Label = "Fair"
                    };
                }
                else
                {
                    // Good #FF00C724
                    return new Indicator()
                    {
                        Symbol = (char)0xE76E,
                        Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xC7, 0x24)),
                        Label = "Good"
                    };
                }
            }
        }

        #endregion

        #region Constructor

        public OverviewPageViewModel()
        {
            // Battery indicators
            _batteryIndicators = new[]
            {
                new[] /* Not Present */
                {
                    (char)0xE996,
                },
                new[] /* Discharging */
                {
                    (char)0xEBA0,
                    (char)0xEBA1,
                    (char)0xEBA2,
                    (char)0xEBA3,
                    (char)0xEBA4,
                    (char)0xEBA5,
                    (char)0xEBA6,
                    (char)0xEBA7,
                    (char)0xEBA8,
                    (char)0xEBA9,
                    (char)0xEBAA,
                },
                new[] /* Idle */
                {
                    (char)0xEBB5,
                },
                new[] /* Charging */
                {
                    (char)0xEBAB,
                    (char)0xEBAC,
                    (char)0xEBAD,
                    (char)0xEBAE,
                    (char)0xEBAF,
                    (char)0xEBB0,
                    (char)0xEBB1,
                    (char)0xEBB2,
                    (char)0xEBB3,
                    (char)0xEBB4,
                    (char)0xEBB5,
                }
            };

            // Load aggregate battery report
            GetAggregateBatteryReport();
        }

        #endregion

        #region Methods



        /// <summary>
        /// Gets the aggregate battery info
        /// </summary>
        /// <returns></returns>
        private void GetAggregateBatteryReport()
        {
            // Get battery info
            var battery = Battery.AggregateBattery;
            if (battery != null)
            {
                // Set the report
                var report = battery.GetReport();
                // Load report
                GetAggregateBatteryStatus(report);
                // Listen for changes
                battery.ReportUpdated += Battery_ReportUpdated;
            }

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
                catch
                {
                }
            }

        }

        /// <summary>
        /// Gets the aggregate battery status
        /// </summary>
        private void GetAggregateBatteryStatus(BatteryReport report)
        {
            // Check if the aggregate report object is null
            if (report == null)
            {
                return;
            }

            // Set properties
            DesignCap = report.DesignCapacityInMilliwattHours;
            FullChargeCap = report.FullChargeCapacityInMilliwattHours;
            RemainingCap = report.RemainingCapacityInMilliwattHours;
            ChargeRate = report.ChargeRateInMilliwatts;
            BatteryStatus = report.Status;

            // Add to charge data
            ChargeData.Add(new ChargeDetail()
            {
                Time = DateTime.Now,
                Value = report.ChargeRateInMilliwatts
            });

        }
        #endregion

        #region Event handlers

        private async void Battery_ReportUpdated(Battery sender, object args)
        {

            await _uiFactory.StartNew(() =>
            {
                // Update the report
                var report = sender.GetReport();
                // Get report info
                GetAggregateBatteryStatus(report);
            });


        }
        #endregion
    }
}
