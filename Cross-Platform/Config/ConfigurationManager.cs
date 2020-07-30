using AttentionAndRetag.Retag;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AttentionAndRetag.Config
{
    public class ConfigurationManager
    {
        private LABEL_FILE? labels;
        const string label_dir = @"Z:\Datasets\bdd100k_labels_release\bdd100k\labels";
        const string img_dir = @"Z:\Datasets\bdd100k_images\bdd100k\images\100k\val";
        const string config_file = "load_config.ini";
        public string? LastOpenedDirectoryLabel { get => cfg["lastOpenedDirectoryLabel"]; set => cfg["lastOpenedDirectoryLabel"] = value; }
        public string? LastOpenedDirectoryImage { get => cfg["lastOpenedDirectoryImage"]; set => cfg["lastOpenedDirectoryImage"] = value; }
        public string? LabelFile { get => cfg["labelFile"]; set => cfg["labelFile"] = value; }
        public bool NeedLabel { get; set; } = true;
        public DirectoryInfo LabelDirectory { get; private set; } = new DirectoryInfo(label_dir);
        public DirectoryInfo ImgDirectory { get; private set; } = new DirectoryInfo(img_dir);

        readonly Dictionary<string, string?> cfg = new Dictionary<string, string?>() { { "lastOpenedDirectoryLabel", null }, { "lastOpenedDirectoryImage", null }, { "labelFile", null } };
        private HashSet<string> allClasses = new HashSet<string> { "traffic sign", "traffic light", "car", "rider", "motor", "person", "bus", "truck", "bike", "train" };

        public void SaveConfig()
        {
            StringWriter sw = new StringWriter();

            foreach (var kv in cfg.OrderBy((x) => x.Key))
                if (kv.Value != null)
                    sw.WriteLine(kv.Key + "=" + kv.Value);

            System.IO.File.WriteAllText(config_file, sw.ToString());
        }

        public void Init() => LoadConfig();
        public IMAGE_LABEL_INFO GetLabel(string filename) => labels.FirstOrDefault((x) => x.name == filename + ".jpg");

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
        public HashSet<string> Classes => allClasses.ToHashSet();
        public bool IsKnownCategory(string s) => allClasses.Contains(s);
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
