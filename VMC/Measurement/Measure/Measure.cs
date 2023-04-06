using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileHelpers;
using VMC.Controller;

namespace VMC.Measurement
{
    public abstract class Measure<TMeasureDomain> where TMeasureDomain : class
    {
        public string Name { get; set; }
        public string DataHeader { get; set; }
        public TimeSpan PreMeasureDelay { get; set; }
        public List<MetaData> MetaData { get; set; }
        public List<IMeasureDevice> MeasureDevices { get; set; } // type of measure device

        protected const string dateFormat = "yyyy-MM-dd";
        protected const string timeFormat = "HH:mm:ss";
        protected const string durationFormat = @"hh\:mm\:ss\.fff";

        protected List<TMeasureDomain> result;
        private readonly FileHelperEngine<TMeasureDomain> engine;


        public Measure(string name)
        {
            Name = name;
            MetaData = new List<MetaData>();
            PreMeasureDelay = new TimeSpan();
            MeasureDevices = null;
            result = new List<TMeasureDomain>();
            engine = new FileHelperEngine<TMeasureDomain>();
        }

        public override string ToString()
        {
            return Name;
        }

        protected static string GetUniqueFilename(string filename)
        {
            int ii = 0;
            string str = filename;
            string dir = Path.GetDirectoryName(filename);
            string fn = Path.GetFileNameWithoutExtension(filename);
            string ext = Path.GetExtension(filename);
            while (File.Exists(str))
            {
                ii++;
                str = $"{dir}\\{fn}_{ii}{ext}";
            }
            return str;
        }

        protected string GetMetaString()
        {
            FileHelperEngine<MetaData> engine = new FileHelperEngine<MetaData>();
            return engine.WriteString(MetaData);
        }

        protected void WriteCSV(string filename)
        {
            engine.HeaderText = $"{GetMetaString()}{System.Environment.NewLine}{DataHeader}";
            engine.WriteFile(filename, result);
        }
    }
}
