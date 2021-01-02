using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
namespace Homiebot.Brain
{
    public class HomiebotContext : DbContext
    {
        public HomiebotContext (DbContextOptions<HomiebotContext> options)
        : base(options)
        {

        }
        public DbSet<MemoryItem> MemoryItems;
        public DbSet<MemoryFile> MemoryFiles;
        public DbSet<ReminderItem> ReminderItems;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MemoryItem>().ToContainer("homiebot").Property(m => m.Key);
            modelBuilder.Entity<MemoryFile>().ToContainer("homiebot").Property(m => m.Key);
            modelBuilder.Entity<ReminderItem>().ToContainer("homiebot").Property(m => m.Key);
        }
    }
}