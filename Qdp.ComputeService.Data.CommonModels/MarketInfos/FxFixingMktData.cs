using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos
{
    [DataContract]
    [Serializable]
    public class FxFixingMktData : MarketDataDefinition
    {
        public FxFixingMktData(string id, double price)
            : base(id)
        {
            Id = id;
            Price = price;
        }

        private FxFixingMktData()
        { 
        }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public double Price { get; set; }
    }
}
