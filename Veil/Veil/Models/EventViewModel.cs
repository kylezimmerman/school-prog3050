using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.ModelBinding;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class EventViewModel : Event
    {
        [DataType(DataType.Date)]
        public new DateTime Date { get; set; }

        [DataType(DataType.Time)]
        public DateTime Time { get; set; }

        [BindNever]
        public DateTime DateTime => new DateTime(Date.Year, Date.Month, Date.Day, Time.Hour, Time.Minute, Time.Second);
    }
}