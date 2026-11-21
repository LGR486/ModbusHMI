using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Models
{
    public class PointItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public ushort Address { get; set; }
        public string Unit { get; set; } = "";
        public double Scale { get; set; } = 1.0;


        private double _value;
        public double Value
        {
            get => _value;
            set { _value = value;OnPropetyChanged(); }
        }
        private double _writevalue;
        public double WriteValue
        {
            get => _writevalue;
            set { _writevalue = value;OnPropetyChanged(); }
        }
        // 以下固定写法
        public event PropertyChangedEventHandler? PropertyChanged;// 更改属性时发消息 用于刷新UI
        protected void OnPropetyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
