using System.Collections.Generic;
using static Newtonsoft.Json.JsonConvert;
namespace AttentionAndRetag.Retag
{
    /// <summary>
    /// Utility class for BDD label
    /// </summary>
    public class LABEL : IClonable<LABEL>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public BOX2D box2d { get; set; } = new BOX2D();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public string category { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot change JSON files")]
        public List<POLY> poly2d { get; set; } = new List<POLY>();
        public LABEL Clone()
        {
            return DeserializeObject<LABEL>(SerializeObject(this));
        }
    }
}
