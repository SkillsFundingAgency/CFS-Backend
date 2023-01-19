using CalculateFunding.Migrations.Fdz.Copy.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CalculateFunding.Migrations.Fdz.Copy
{
    internal class DataSource
    {
        private string _sqlConnectionString;
        private int _providerSnapshotId;

        public DataSource(string sqlConnectionString, int providerSnapshotId)
        {
            _sqlConnectionString = sqlConnectionString;
            _providerSnapshotId = providerSnapshotId;
        }

        internal ProviderSnapshot? GetSnapshot()
        {
            ProviderSnapshot? providerSnapshot = null;

            using (SqlConnection connection = new SqlConnection(_sqlConnectionString))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = $"SELECT * FROM ProviderSnapshot WHERE ProviderSnapshotId = {_providerSnapshotId}";

                    connection.Open();

                    SqlDataReader? reader = command.ExecuteReader();
                    Func<System.Data.IDataReader, ProviderSnapshot>? parser = reader.GetRowParser<ProviderSnapshot>(typeof(ProviderSnapshot));

                    if (reader.Read())
                    {
                        providerSnapshot = parser(reader);
                    }
                }
            }

            return providerSnapshot;
        }

        internal ProviderSnapshotPeriod? GetSnapshotPeriod()
        {
            ProviderSnapshotPeriod? providerSnapshotPeriod = null;

            using (SqlConnection connection = new SqlConnection(_sqlConnectionString))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = $"SELECT * FROM ProviderSnapshotPeriod WHERE ProviderSnapshotId = {_providerSnapshotId}";

                    connection.Open();

                    SqlDataReader? reader = command.ExecuteReader();
                    Func<System.Data.IDataReader, ProviderSnapshotPeriod>? parser = reader.GetRowParser<ProviderSnapshotPeriod>(typeof(ProviderSnapshotPeriod));

                    if (reader.Read())
                    {
                        providerSnapshotPeriod = parser(reader);
                    }
                }
            }

            return providerSnapshotPeriod;
        }

        internal List<Provider> GetProviders()
        {
            List<Provider> providers = new List<Provider>();

            using (SqlConnection connection = new SqlConnection(_sqlConnectionString))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = $"SELECT * FROM Provider WHERE ProviderSnapshotId = {_providerSnapshotId}";

                    connection.Open();

                    SqlDataReader? reader = command.ExecuteReader();
                    Func<System.Data.IDataReader, Provider>? parser = reader.GetRowParser<Provider>(typeof(Provider));

                    while (reader.Read())
                    {
                        Provider? theObject = parser(reader);
                        if (theObject != null)
                        {
                            providers.Add(theObject);
                        }
                    }
                }
            }

            return providers;
        }

        internal List<Predecessor> GetProviderPredecessors()
        {
            return GetProviderRelationships<Predecessor>();
        }

        internal List<Successor> GetSuccessors()
        {
            return GetProviderRelationships<Successor>();
        }

        internal List<T> GetProviderRelationships<T>() where T : ProviderRelationship
        {
            List<T> relationships = new List<T>();
            T relationship = (T)Activator.CreateInstance(typeof(T));

            using (SqlConnection connection = new SqlConnection(_sqlConnectionString))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = $"SELECT distinct pre.* FROM Provider p INNER JOIN {relationship.TableName} pre ON p.Id = pre.ProviderId WHERE p.ProviderSnapshotId = {_providerSnapshotId}";

                    connection.Open();

                    SqlDataReader? reader = command.ExecuteReader();
                    Func<System.Data.IDataReader, T>? parser = reader.GetRowParser<T>(typeof(T));

                    while (reader.Read())
                    {
                        T? theObject = parser(reader);
                        if (theObject != null)
                        {
                            relationships.Add(theObject);
                        }
                    }
                }
            }

            return relationships;
        }

        internal List<PaymentOrganisation> GetPaymentOrganisations()
        {
            List<PaymentOrganisation> paymentOrganisations = new List<PaymentOrganisation>();

            using (SqlConnection connection = new SqlConnection(_sqlConnectionString))
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = $"SELECT DISTINCT po.* FROM PaymentOrganisation po INNER JOIN Provider p on p.PaymentOrganisationId = po.PaymentOrganisationId WHERE p.ProviderSnapshotId = {_providerSnapshotId}";

                    connection.Open();

                    SqlDataReader? reader = command.ExecuteReader();
                    Func<System.Data.IDataReader, PaymentOrganisation>? parser = reader.GetRowParser<PaymentOrganisation>(typeof(PaymentOrganisation));

                    while (reader.Read())
                    {
                        PaymentOrganisation? theObject = parser(reader);
                        if (theObject != null)
                        {
                            paymentOrganisations.Add(theObject);
                        }
                    }
                }
            }

            return paymentOrganisations;
        }
    }
}
