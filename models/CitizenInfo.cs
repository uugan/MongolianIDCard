using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonSign.models
{
    public class CitizenInfo
    {
        public string new_reg_num { get; set; }
        public string address { get; set; }
        public string reg_num { get; set; }
        public string DOI { get; set; }
        public string DOE { get; set; }
        public string gender { get; set; }
        public string surname { get; set; }
        public string givenname { get; set; }
        public string familyname { get; set; }
        public string id_card_number { get; set; }
        public string birthday { get; set; }
        public string issuer { get; set; }
        public string birth_place { get; set; }

        //JPEG2000 byte array
        public byte[] portrait { get; set; }


    }
}
