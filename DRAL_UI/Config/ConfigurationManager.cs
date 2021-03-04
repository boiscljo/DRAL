using AttentionAndRetag.Retag;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AttentionAndRetag.Config
{
    /// <summary>
    /// Contains all app configuration
    /// </summary>
    public class ConfigurationManager
    {
        /// <summary>
        /// BDD file in memory
        /// </summary>
        private LABEL_FILE? labels;
        /// <summary>
        /// Location for default
        /// </summary>
        const string label_dir = @"Z:\Datasets\bdd100k_labels_release\bdd100k\labels";
        /// <summary>
        /// Location for default
        /// </summary>
        const string img_dir = @"Z:\Datasets\bdd100k_images\bdd100k\images\100k\val";
        /// <summary>
        /// Name of configuration file
        /// </summary>
        const string config_file = "load_config.ini";
        /// <summary>
        /// Keeping tracks
        /// </summary>
        public string? LastOpenedDirectoryLabel { get => cfg["lastOpenedDirectoryLabel"]; set => cfg["lastOpenedDirectoryLabel"] = value; }
        /// <summary>
        /// Keeping tracks
        /// </summary>
        public string? LastOpenedDirectoryImage { get => cfg["lastOpenedDirectoryImage"]; set => cfg["lastOpenedDirectoryImage"] = value; }
        /// <summary>
        /// Keeping tracks
        /// </summary>
        public string? LabelFile { get => cfg["labelFile"]; set => cfg["labelFile"] = value; }
        /// <summary>
        /// Indicate if a Label file is required in the current mode
        /// </summary>
        public bool NeedLabel { get; set; } = true;
        /// <summary>
        /// Directory where Labels are located
        /// </summary>
        public DirectoryInfo LabelDirectory { get; private set; } = new DirectoryInfo(label_dir);
        /// <summary>
        /// Directory where Images are located
        /// </summary>
        public DirectoryInfo ImgDirectory { get; private set; } = new DirectoryInfo(img_dir);
        /// <summary>
        /// Configuration in a dictionnary 
        /// </summary>
        readonly Dictionary<string, string?> cfg = new Dictionary<string, string?>() { { "lastOpenedDirectoryLabel", null }, { "lastOpenedDirectoryImage", null }, { "labelFile", null } };
        /// <summary>
        /// Store all classes available 
        /// </summary>
        private HashSet<string> allClasses = new HashSet<string> { "traffic sign", "traffic light", "car", "rider", "motor", "person", "bus", "truck", "bike", "train" };
        /// <summary>
        /// Save the current configuration to file
        /// </summary>
        public void SaveConfig()
        {
            StringWriter sw = new StringWriter();

            foreach (var kv in cfg.OrderBy((x) => x.Key))
                if (kv.Value != null)
                    sw.WriteLine(kv.Key + "=" + kv.Value);

            System.IO.File.WriteAllText(config_file, sw.ToString());
        }
        /// <summary>
        /// Init = Load
        /// </summary>
        public void Init() => LoadConfig();
        /// <summary>
        /// Get a label from an image name
        /// </summary>
        /// <param name="filename">image name</param>
        /// <returns>Label for that image</returns>
        public IMAGE_LABEL_INFO GetLabel(string filename) => labels?.FirstOrDefault((x) => x.name == filename + ".jpg");
        /// <summary>
        /// Load the current configuration from file, INI style
        /// </summary>
        public void LoadConfig()
        {
            FileInfo fi = new FileInfo(config_file);
            if (fi.Exists)
            {
                StringReader sr = new StringReader(System.IO.File.ReadAllText(config_file));
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    var splt = line.Split('=');
                    if (splt.Length >= 2)
                    {
                        var key = splt[0];
                        var val = string.Join('=', splt.Skip(1)).Split('#').ElementAt(0).Trim();
                        var element = val;
                        if (val.Length > 0)
                            cfg[key] = element;
                    }
                }
            }
            if (LabelFile != null && NeedLabel)
            {
                LoadLabels(LabelFile);
            }
        }
        /// <summary>
        /// Load the labels in memory, huge
        /// </summary>
        /// <param name="labelFile">Label JSON file</param>
        public void LoadLabels(string labelFile)
        {
            try
            {
                labels = Newtonsoft.Json.JsonConvert.DeserializeObject<LABEL_FILE>(System.IO.File.ReadAllText(labelFile));

                LabelFile = labelFile;
                LastOpenedDirectoryLabel = new FileInfo(labelFile).Directory.FullName;
            }
            catch
            {
            }
        }
        /// <summary>
        /// Generate class list from file
        /// </summary>
        public void GenerateAllClasses()
        {
            HashSet<string> elements = new HashSet<string>();
            foreach (var img in labels)
                foreach (var box in img.labels)
                    if (box.box2d != null)
                        if (!elements.Contains(box.category))
                            elements.Add(box.category);
            allClasses = elements;
        }
        /// <summary>
        /// Creates a copy of the classes list
        /// </summary>
        public HashSet<string> Classes => allClasses.ToHashSet();
        /// <summary>
        /// Is the string in Classes
        /// </summary>
        /// <param name="s">String to detect</param>
        /// <returns>Presence</returns>
        public bool IsKnownCategory(string s) => allClasses.Contains(s);
        /// <summary>
        /// Convert categories to number
        /// </summary>
        /// <param name="s">Category name</param>
        /// <returns>ID</returns>
        public int GETKnownCategoryID(string s)
        {
            return s switch
            {
                "traffic sign" => 0,
                "traffic light" => 0,
                "car" => 1,
                "rider" => 2,
                "motor" => 1,
                "person" => 3,
                "bus" => 1,
                "truck" => 1,
                "bike" => 2,
                "train" => 1,
                _ => -1,
            };
        }
    }
}
