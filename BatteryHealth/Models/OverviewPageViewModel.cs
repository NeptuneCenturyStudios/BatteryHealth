using BatteryHealth.DataModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
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

        public ObservableCollection<Battery> BatteryDevices = new ObservableCollection<Battery>();

        private BatteryDetail[] _aggregateDetails;
        public BatteryDetail[] AggregateDetails
        {
            get { return _aggregateDetails; }
            set
            {
                _aggregateDetails = value;
                // Notify change
                OnPropertyChanged(nameof(AggregateDetails));
            }
        }

        //private BatteryReport _aggregateReport;
        ///// <summary>
        /////  Gets or sets the aggregate battery report
        ///// </summary>
        //public BatteryReport AggregateReport
        //{
        //    get { return _aggregateReport; }
        //    set
        //    {
        //        _aggregateReport = value;
        //        // Notify change
        //        OnPropertyChanged(nameof(AggregateReport));
        //    }
        //}

        private int? _aggregateDesignCap;

        public int? AggregateDesignCap
        {
            get { return _aggregateDesignCap; }
            set
            {
                _aggregateDesignCap = value;
                // Notify change
                OnPropertyChanged(nameof(AggregateDesignCap));
            }
        }

        private int? _aggregateFullChargeCap;

        public int? AggregateFullChargeCap
        {
            get { return _aggregateFullChargeCap; }
            set
            {
                _aggregateFullChargeCap = value;
                // Notify change
                OnPropertyChanged(nameof(AggregateFullChargeCap));
            }
        }


        private int? _aggregateRemainingCap;

        public int? AggregateRemainingCap
        {
            get { return _aggregateRemainingCap; }
            set
            {
                _aggregateRemainingCap = value;
                // Notify change
                OnPropertyChanged(nameof(AggregateRemainingCap));

            }
        }

        private Windows.System.Power.BatteryStatus _aggregateStatus;

        public Windows.System.Power.BatteryStatus AggregateStatus
        {
            get { return _aggregateStatus; }
            set
            {
                _aggregateStatus = value;
                // Notify change
                OnPropertyChanged(nameof(AggregateStatus));
            }
        }


        private string _formattedAggregateStatus;
        /// <summary>
        /// Gets or sets the aggregate status
        /// </summary>
        public string FormattedAggregateStatus
        {
            get { return _formattedAggregateStatus; }
            set
            {
                _formattedAggregateStatus = value;
                // Notify change
                OnPropertyChanged(nameof(FormattedAggregateStatus));
            }
        }

        private decimal? _aggregatePercent;
        /// <summary>
        /// Gets or sets the percentage of the battery
        /// </summary>
        public decimal? AggregatePercent
        {
            get { return _aggregatePercent; }
            set
            {
                _aggregatePercent = value;
                // Notify change
                OnPropertyChanged(nameof(AggregatePercent));
            }
        }

        public char AggregateBatteryIndicator
        {
            get
            {
                // Get the battery status
                var index = (int)(AggregatePercent.GetValueOrDefault() / 10);

                // If the status is idle, index is 0
                if (AggregateStatus == Windows.System.Power.BatteryStatus.Idle)
                {
                    index = 0;
                }

                // Get indicator
                var indicator = _batteryIndicators[(int)AggregateStatus][index];

                return indicator;

            }
        }

        /// <summary>
        /// Gets a formatted string of the AggregatePercent
        /// </summary>
        public string FormattedAggregatePercent
        {
            get
            {
                if (AggregatePercent != null)
                {
                    return $"({AggregatePercent}%)";
                }
                else { return null; }
            }
        }

        /// <summary>
        /// Gets the overall efficiency of the battery
        /// </summary>
        public double? Efficiency
        {
            get
            {
                return AggregateFullChargeCap / (double?)AggregateDesignCap;
            }
        }

        public string EfficiencyStatus
        {
            get
            {
                if (Efficiency != null && Efficiency <= 0.25)
                {
                    return $"Your battery can only hold {Efficiency * 100}% of its designed capacity. Please replace your battery.";
                }
                else if (Efficiency != null && Efficiency <= 0.50)
                {
                    return $"Your battery can only hold {Efficiency * 100}% of its designed capacity. Consider replacing your battery.";
                }
                else
                {
                    return $"Your battery is holding {Efficiency * 100}% of its designed capacity.";
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
                // FF4AFF7B
                if (Efficiency != null && Efficiency <= 0.25)
                {
                    // Critical
                    return new Indicator()
                    {
                        Symbol = (char)0xE996,
                        Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x4A, 0x4A)),
                        Label = "Critical"
                    }; 
                }
                else if (Efficiency != null && Efficiency <= 0.50)
                {
                    // Poor
                    return new Indicator()
                    {
                        Symbol = (char)0xE8C9,
                        Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xA4, 0x4A)),
                        Label = "Poor"
                    };
                }
                else if (Efficiency != null && Efficiency <= 0.50)
                {
                    // Fair
                    return new Indicator()
                    {
                        Symbol = (char)0xE76E,
                        Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x4A, 0xFF, 0x7B)),
                        Label = "Fair"
                    };
                }
                else
                {
                    // Good
                    return new Indicator()
                    {
                        Symbol = (char)0xE899,
                        Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x4A, 0xFF, 0x7B)),
                        Label = "Good"
                    };
                }
            }
        }

        #endregion

        #region Constructor

        public OverviewPageViewModel()
        {
            // Load aggregate battery report
            GetAggregateBatteryReport();

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
                catch { /* Add error handling, as applicable */ }
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
            AggregateDesignCap = report.DesignCapacityInMilliwattHours ?? 1;
            AggregateFullChargeCap = report.FullChargeCapacityInMilliwattHours ?? 1;
            AggregateRemainingCap = report.RemainingCapacityInMilliwattHours ?? 1;
            AggregateStatus = report.Status;

            // Set up chart series
            //AggregateDetails = new BatteryDetail[]
            //{
            //    new BatteryDetail
            //    {
            //        Name = "Design Capacity (mWH)",
            //        Value = AggregateDesignCap - AggregateFullChargeCap
            //    },
            //    new BatteryDetail
            //    {
            //        Name = "Full Charge Capacity (mWH)",
            //        Value = AggregateFullChargeCap - AggregateRemainingCap
            //    },
            //    new BatteryDetail
            //    {
            //        Name = "Remaining Capacity (mWH)",
            //        Value = AggregateRemainingCap
            //    }
            //};


            // If the MilliwattHours properties are null, assume 100 percent
            var remainingPercent = (decimal?)AggregateRemainingCap / (decimal?)AggregateFullChargeCap;

            // Determine status
            switch (AggregateStatus)
            {
                case Windows.System.Power.BatteryStatus.Charging:
                    FormattedAggregateStatus = "Charging";
                    break;

                case Windows.System.Power.BatteryStatus.Discharging:
                    FormattedAggregateStatus = "Discharging";
                    break;

                case Windows.System.Power.BatteryStatus.Idle:
                    FormattedAggregateStatus = "Not Charging";
                    break;

                case Windows.System.Power.BatteryStatus.NotPresent:
                    FormattedAggregateStatus = "Not Present";
                    break;

                default:
                    break;
            }



            // Calculate percentage
            AggregatePercent = remainingPercent * 100;
        }
        #endregion

        #region Event handlers

        private async void Battery_ReportUpdated(Battery sender, object args)
        {

            await _uiFactory.StartNew(() =>
            {
                // Update the report
                var report = sender.GetReport();
                // Determine status
                GetAggregateBatteryStatus(report);
            });


        }
        #endregion
    }
}
