using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QotomReview.model
{
    public class ConfigData
    {
        public string OSVer { get; set; }
        public string OSType { get; set; }
        public string Language { get; set; }
        public string Processor { get; set; }
        public string Motherboard { get; set; }
        public string BIOS { get; set; }
        public string[] Memory { get; set; }
        public string[] Storage { get; set; }
        public string[] Network { get; set; }

        public ConfigData()
        {
        }

        public override string ToString()
        {
            return $"{{{nameof(OSVer)}={OSVer}, {nameof(OSType)}={OSType}, {nameof(Language)}={Language}, {nameof(Processor)}={Processor}, {nameof(Motherboard)}={Motherboard}, {nameof(BIOS)}={BIOS}, {nameof(Memory)}={Memory}, {nameof(Storage)}={Storage}, {nameof(Network)}={Network}}}";
        }
    }
}
