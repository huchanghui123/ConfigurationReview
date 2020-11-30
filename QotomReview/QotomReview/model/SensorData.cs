using System.ComponentModel;

namespace QotomReview.model
{
    public class SensorData : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Check { get; set; }
        string _value;
        string _minValue;
        string _maxValue;

        public event PropertyChangedEventHandler PropertyChanged;

        public SensorData(string name, string value, string minValue, string maxValue)
        {
            Name = name;
            _value = value;
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public string Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }
        public string MinValue
        {
            get { return _minValue; }
            set
            {
                _minValue = value;
                OnPropertyChanged("MinValue");
            }
        }
        public string MaxValue
        {
            get { return _maxValue; }
            set
            {
                _maxValue = value;
                OnPropertyChanged("MaxValue");
            }
        }

        //动态更新数据
        protected internal virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
