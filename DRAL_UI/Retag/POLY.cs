using System.Collections.Generic;
using static Newtonsoft.Json.JsonConvert;
namespace AttentionAndRetag.Retag
{
    /// <summary>
    /// Utility class for BDD label
    /// </summary>
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
