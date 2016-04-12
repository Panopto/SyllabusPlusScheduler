using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyllabusPlusSchedulerService.DB
{
    internal class SyllabusPlusDBContext : DbContext
    {
        public DbSet<Schedule> Schedules { get; set; }
    }
}
