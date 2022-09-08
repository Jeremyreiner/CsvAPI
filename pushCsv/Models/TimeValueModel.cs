using System;
using System.Collections.Generic;
using InfluxDB.Client.Core;

namespace InfluxCsvReal.Models
{
    [Measurement("dynamic")]
    public class TimeValueModel
    {

        [Column(IsTimestamp = true)] public DateTime? Time { get; set; }

        [Column("value")] public double Value { get; set; }

        [Column("title", IsTag = true)] public string Title { get; set; }

        [Column("column")] public int Column { get; set; }

        public override string ToString() => $"{Time:G} {Value}";

        public TimeValueModel Clone()
        {
            return new TimeValueModel
            {
                Column = Column,
                Title = Title,
                Time = Time,
                Value = Value
            };
        }

        public List<string> ToStringList()
        {
            return new List<string>
            {
                this.Title.ToString(),
                this.Time.ToString(),
                this.Value.ToString()
            };
        }
    }
}
