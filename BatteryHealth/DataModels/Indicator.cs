using System;
using System.Collections.Generic;
using Windows.UI;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace BatteryHealth.DataModels
{
    class Indicator
    {
        public char Symbol { get; set; }
        public Brush Foreground { get; set; }
        public string Label { get; set; }
    }
}
