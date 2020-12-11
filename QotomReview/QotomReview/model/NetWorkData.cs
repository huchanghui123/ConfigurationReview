namespace QotomReview.model
{
    public class NetWorkData
    {
        public string Adapter { get; set; }
        public string Type { get; set; }
        public string Mac { get; set; }
        public string Ip { get; set; }
        public string Status { get; set; }
        public string Check { get; set; }

        public NetWorkData()
        {
        }

        public NetWorkData(string adapter, string type, string mac, string ip, string status)
        {
            this.Adapter = adapter;
            this.Type = type;
            this.Mac = mac;
            this.Ip = ip;
            this.Status = status;
        }

        public override string ToString()
        {
            return $"{{{nameof(Adapter)}={Adapter}, {nameof(Type)}={Type}, {nameof(Mac)}={Mac}, {nameof(Ip)}={Ip}, {nameof(Status)}={Status}, {nameof(Check)}={Check}}}";
        }
    }
}
