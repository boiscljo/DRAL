using System.Collections.Generic;
using static Newtonsoft.Json.JsonConvert;
namespace AttentionAndRetag.Retag
{
    /// <summary>
    /// Utility class for BDD label
    /// </summary>
    public class LABEL_FILE : List<IMAGE_LABEL_INFO>, IClonable<LABEL_FILE>
    {
        public LABEL_FILE Clone()
        {
            return DeserializeObject<LABEL_FILE>(SerializeObject(this));
        }
    }
}
