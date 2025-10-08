using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bc3_WPF.Backend.Modelos
{
    public class Material
    {
        public string? id {  get; set; }
        public string internalId { get; set; }
        public string description { get; set; }
        public string category { get; set; }
        public double factor { get; set; }
        public string indicator { get; set; }
        public double value { get; set; }

        public Material(string id, string internalId, string category, string description, double factor, double value, string indicator)
        {
            this.id = id;
            this.internalId = internalId;
            this.description = description;
            this.category = category;
            this.factor = factor;
            this.value = value;
            this.indicator = indicator;
        } 

    }
}
