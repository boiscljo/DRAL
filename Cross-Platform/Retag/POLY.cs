using System.Collections.Generic;
using static Newtonsoft.Json.JsonConvert;
namespace AttentionAndRetag.Retag
{
    public class POLY : IClonable<POLY>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public List<List<double>> vertices { get; set; }
        public POLY Clone()
        {
            return DeserializeObject<POLY>(SerializeObject(this));
        }
    }
}
