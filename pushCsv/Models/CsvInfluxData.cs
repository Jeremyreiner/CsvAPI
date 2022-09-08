//using System;
//using InfluxDB.Client.Core;

//namespace InfluxCsvReal.Models
//{
//    [Measurement("csvdemo")]
//    public class CsvInfluxData
//    {
//        [Column("EMS", IsTag = true)] public string EmsId { get; set; }

//        [Column(IsTimestamp = true)] public DateTime Time { get; set; }
//        // TIMESTAMP DATETIME NEEDS TO BE IN UTC FORMAT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

//        [Column("EngineRpmTarget")] public float EngineRpmTarget { get; set; }

//        [Column("Rpm")] public float Rpm { get; set; }

//        [Column("PmgSpeed")] public float PmgSpeed { get; set; }
//    }
//}