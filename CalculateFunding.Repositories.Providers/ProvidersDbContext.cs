using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Providers;
using FastMember;
using Microsoft.EntityFrameworkCore;

namespace CalculateFunding.Repositories.Providers
{

    public static class ExtensionMethods
    {
        private static List<Type> Types
        {
            get
            {
                return new List<Type>
                {
                    typeof (String),
                    typeof (int?),
                    typeof (Guid?),
                    typeof (double?),
                    typeof (decimal?),
                    typeof (float?),
                    typeof (Single?),
                    typeof (bool?),
                    typeof (DateTime?),
                    typeof (DateTimeOffset?),
                    typeof (int),
                    typeof (Guid),
                    typeof (double),
                    typeof (decimal),
                    typeof (float),
                    typeof (Single),
                    typeof (bool),
                    typeof (DateTime),
                    typeof (DateTimeOffset),
                    typeof (DBNull)
                };
            }
        }

        public static IEnumerable<SqlBulkCopyColumnMapping> GetColumnMappings<T>(this IEnumerable<T> source)
        {
            return typeof(T).GetProperties().Where(x => Types.Contains(x.PropertyType)).Select(x => new SqlBulkCopyColumnMapping(x.Name, x.Name));
        }


        public static DataTable ToDataTable<T>(this IEnumerable<T> source)
        {
            using (var dt = new DataTable())
            {
                var toList = source.ToList();

                var properties = typeof(T).GetProperties();

                for (var index = 0; index < properties.Count(); index++)
                {
                    var info = typeof(T).GetProperties()[index];
                    if (Types.Contains(info.PropertyType))
                    {
                        var type = (info.PropertyType.IsGenericType && info.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(info.PropertyType) : info.PropertyType);

                        dt.Columns.Add(new DataColumn(info.Name, type));
                    }
                }

                for (var index = 0; index < toList.Count; index++)
                {
                    var t = toList[index];
                    var row = dt.NewRow();
                    foreach (var info in properties)
                    {
                        if (Types.Contains(info.PropertyType))
                        {   
                            
                            row[info.Name] = info.GetValue(t, null) ?? DBNull.Value;
                        }
                    }
                    dt.Rows.Add(row);
                }

                return dt;
            }
        }
    }
    public class ProvidersDbContext : DbContext
    {

        
        public async Task BulkInsert<T>(string tableName, IEnumerable<T> entities)
        {
            var connection = Database.GetDbConnection() as SqlConnection;
            if (connection.State != ConnectionState.Open)
            {
                await Database.OpenConnectionAsync();
            }

            using (var bcp = new SqlBulkCopy(connection))
            {
                bcp.BulkCopyTimeout = 60 * 30;
                var columnMappings = entities.GetColumnMappings();
                foreach (var columnMapping in columnMappings)
                {
                    bcp.ColumnMappings.Add(columnMapping);
                }

                bcp.DestinationTableName = tableName;
                var table = entities.ToDataTable();
                await bcp.WriteToServerAsync(table);
            }
        }



        public ProvidersDbContext(DbContextOptions options) : base (options)
        {
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlServer(
            //    "Server=.;Database=Providers;Trusted_Connection=True;MultipleActiveResultSets=true",
            //    b => b.MigrationsAssembly("CalculateFunding.Repositories.Providers.Migrations"));
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProviderEntity>()
                .HasKey(c => c.URN);


            modelBuilder.Entity<ProviderEntity>()
                .HasIndex(b => b.URN);

            modelBuilder.Entity<ProviderCommandCandidate>()
                .HasKey(c => new { c.ProviderCommandId, c.URN });


            modelBuilder.Entity<ProviderCommandCandidate>()
                .HasIndex(b => new{ b.ProviderCommandId, b.URN});

        }

        public DbSet<ProviderEntity> Providers { get; set; }
        public DbSet<ProviderCommand> ProviderCommands { get; set; }
        public DbSet<ProviderCommandCandidate> ProviderCommandCandidates { get; set; }
    }

    public abstract class DbEntity
    {

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public bool Deleted { get; set; }

        [Timestamp]
        public byte[] Timestamp { get; set; }
    }

    public class ProviderCommand : DbEntity
    {
        public Guid Id { get; set; }
    }


    public class ProviderCommandCandidate : ProviderBaseEntity
    {
        public Guid ProviderCommandId { get; set; }

        public string URN { get; set; }

        public virtual ProviderCommand ProviderCommand {get; set; }
    }

    public class ProviderEntity : ProviderBaseEntity
    {
        public string URN { get; set; }
    }

    public abstract class ProviderBaseEntity : DbEntity
    {
        
        public string URN { get; set; }

        public string UKPRN { get; set; }

        public string Name { get; set; }

        //public Reference Authority { get; set; }

        public DateTimeOffset? OpenDate { get; set; }
        public DateTimeOffset? CloseDate { get; set; }

        public string EstablishmentNumber { get; set; }
        public string EstablishmentName { get; set; }
        //public Reference EstablishmentType { get; set; }
        //public Reference EstablishmentTypeGroup { get; set; }
        //public Reference EstablishmentStatus { get; set; }
        //public Reference ReasonEstablishmentOpened { get; set; }
        //public Reference ReasonEstablishmentClosed { get; set; }
        //public Reference PhaseOfEducation { get; set; }
        public int? StatutoryLowAge { get; set; }
        public int? StatutoryHighAge { get; set; }
        //public Reference Boarders { get; set; }
        //public Reference NurseryProvision { get; set; }
        //public Reference OfficialSixthForm { get; set; }
        //public Reference Gender { get; set; }
        //public Reference ReligiousCharacter { get; set; }
        //public Reference ReligiousEthos { get; set; }
        //public Reference Diocese { get; set; }
        //public Reference AdmissionsPolicy { get; set; }
        public int? SchoolCapacity { get; set; }
        //public Reference SpecialClasses { get; set; }
        public DateTimeOffset? CensusDate { get; set; }
        public int? NumberOfPupils { get; set; }
        public int? NumberOfBoys { get; set; }
        public int? NumberOfGirls { get; set; }

        public decimal? PercentageFSM { get; set; }
        //public Reference TrustSchoolFlag { get; set; }
        //public Reference Trusts { get; set; }
        public string SchoolSponsorFlag { get; set; }
        public string SchoolSponsors { get; set; }
        public string FederationFlag { get; set; }
        //public Reference Federations { get; set; }
        public string FEHEIdentifier { get; set; }
        public string FurtherEducationType { get; set; }
        public DateTimeOffset? OfstedLastInspectionDate { get; set; }
        //public Reference OfstedSpecialMeasures { get; set; }
        public string OfstedRating { get; set; }
        public DateTimeOffset? LastChangedDate { get; set; }

        public string Street { get; set; }
        public string Locality { get; set; }
        public string Address3 { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Postcode { get; set; }
        public string Country { get; set; }
        public int? Easting { get; set; }
        public int? Northing { get; set; }
        public string Website { get; set; }
        public string Telephone { get; set; }
        public string TeenMoth { get; set; }
        public int? TeenMothPlaces { get; set; }

        public string CCF { get; set; }
        public string SENPRU { get; set; }
        public string EBD { get; set; }
        public int? PRUPlaces { get; set; }
        public string FTProv { get; set; }
        public string EdByOther { get; set; }
        public string Section41Approved { get; set; }
        public string SEN1 { get; set; }
        public string TypeOfResourcedProvision { get; set; }
        public int? ResourcedProvisionOnRoll { get; set; }
        public int? ResourcedProvisionCapacity { get; set; }
        public int? SenUnitOnRoll { get; set; }
        public int? SenUnitCapacity { get; set; }
        //public Reference GOR { get; set; }
        //public Reference DistrictAdministrative { get; set; }
        //public Reference AdministrativeWard { get; set; }
        //public Reference ParliamentaryConstituency { get; set; }
        //public Reference UrbanRural { get; set; }
        public string GSSLACode { get; set; }
        public string CensusAreaStatisticWard { get; set; }
        public string MSOA { get; set; }
        public string LSOA { get; set; }
        public int? SENStat { get; set; }
        public int? SENNoStat { get; set; }
        public string RSCRegion { get; set; }
    }
}
