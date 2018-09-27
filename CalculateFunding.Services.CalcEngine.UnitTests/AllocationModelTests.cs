using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class AllocationModelTests
    {
        [TestMethod]
        public void Execute_GivenAssembly_Executes()
        {
            //Arrange
            Assembly assembly = CreateAssembly();

            AllocationModel allocationModel = new AllocationFactory().CreateAllocationModel(assembly) as AllocationModel;

            IEnumerable<ProviderSourceDataset> sourceDatasets = CreateProviderSourceDatasets();

            ProviderSummary providerSummary = CreateProviderSummary();

            //Act
            IEnumerable<CalculationResult> calcResults = allocationModel.Execute(sourceDatasets.ToList(), providerSummary);

            //Assert
            calcResults.Any().Should().BeTrue();
            calcResults.Count().Should().Be(6);
            calcResults.First(r => r.Calculation.Name == "AB Calc 2109").Value.Should().Be(59);
            calcResults.First(r => r.Calculation.Name == "AB Calc 2509").Value.Should().Be(112);
            calcResults.First(r => r.Calculation.Name == "AB Calc 2509-002").Value.Should().BeLessThan(1);
            calcResults.First(r => r.Calculation.Name == "AB Calc 2609-001").Value.Should().Be(11);
            calcResults.First(r => r.Calculation.Name == "AB Clc 2609-0002").Value.Should().Be(10079319M);
            calcResults.First(r => r.Calculation.Name == "ABCalcExclude-2609-001").Value.Should().BeNull();
        }

        [TestMethod]
        public void Execute_GivenAssembly_EnsuresBindingOfDataset()
        {
            //Arrange
            Assembly assembly = CreateAssembly();

            AllocationModel allocationModel = new AllocationFactory().CreateAllocationModel(assembly) as AllocationModel;

            IEnumerable<ProviderSourceDataset> sourceDatasets = CreateProviderSourceDatasets();

            ProviderSummary providerSummary = CreateProviderSummary();

            //Act
            IEnumerable<CalculationResult> calcResults = allocationModel.Execute(sourceDatasets.ToList(), providerSummary);

            //Assert
            calcResults.Any().Should().BeTrue();

            dynamic instance = allocationModel.Instance as dynamic;

            Assert.IsNotNull(instance.Datasets);
            
            Assert.AreEqual(instance.Datasets.ABPE2109001.URN, "100000");
            Assert.AreEqual(instance.Datasets.ABPE2109001.LocalAuthority, "201");
            Assert.AreEqual(instance.Datasets.ABPE2109001.EstablishmentNumber, "3614");
            Assert.AreEqual(instance.Datasets.ABPE2109001.LAEstab, "2013614");
            Assert.AreEqual(instance.Datasets.ABPE2109001.SchoolType, "Voluntary aided school");
            Assert.AreEqual(instance.Datasets.ABPE2109001.AcademyType, "Not Applicable");
            Assert.AreEqual(instance.Datasets.ABPE2109001.PhaseOfEducation, "Primary");
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup1SoleRegistrations, 10);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup2SoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup3SoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup4SoleRegistrations, 11);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup5SoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup6SoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroupNotFollowedAge5To10SoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup1SoleRegistrations, 59);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup2SoleRegistrations, 30);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup3SoleRegistrations, 31);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup4SoleRegistrations, 30);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup5SoleRegistrations, 30);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup6SoleRegistrations, 29);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroupNotFollowedAge5To10SoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup1DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup2DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup3DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup4DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup5DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroup6DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimeNumberOfPupilsInYearGroupNotFollowedAge5To10DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup1DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup2DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup3DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup4DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup5DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroup6DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimeNumberOfPupilsInYearGroupNotFollowedAge5To10DualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged5HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged6HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged6HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged8HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged9HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged10HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged5HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged6HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged7HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged8HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged9HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged10HospitalSchoolsSoleRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged5HosptialSchoolsDualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged6HosptialSchoolsDualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged7HosptialSchoolsDualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged8HosptialSchoolsDualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged9HosptialSchoolsDualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.PartTimePupilsAged10HosptialSchoolsDualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged5HosptialSchoolsDualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged6HosptialSchoolsDualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged7HosptialSchoolsDualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged8HosptialSchoolsDualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged9HosptialSchoolsDualRegistrations, 0);
            Assert.AreEqual(instance.Datasets.ABPE2109001.FullTimePupilsAged10HosptialSchoolsDualRegistrations, 0);
        }

        [TestMethod]
        public void Execute_GivenAssembly_EnsuresBindingOfProvider()
        {
            //Arrange
            Assembly assembly = CreateAssembly();

            AllocationModel allocationModel = new AllocationFactory().CreateAllocationModel(assembly) as AllocationModel;

            IEnumerable<ProviderSourceDataset> sourceDatasets = CreateProviderSourceDatasets();

            ProviderSummary providerSummary = CreateProviderSummary();

            //Act
            IEnumerable<CalculationResult> calcResults = allocationModel.Execute(sourceDatasets.ToList(), providerSummary);

            //Assert
            calcResults.Any().Should().BeTrue();

            dynamic instance = allocationModel.Instance as dynamic;

            Assert.IsNotNull(instance.Provider);
            Assert.AreEqual(instance.Provider.ProviderType, "Voluntary aided school");
            Assert.AreEqual(instance.Provider.ProviderSubType, "Not Applicable");
            Assert.AreEqual(instance.Provider.Authority, "authority");
            Assert.AreEqual(instance.Provider.UKPRN, "10079319");
            Assert.AreEqual(instance.Provider.UPIN, "12345");
            Assert.AreEqual(instance.Provider.URN, "100000");
            Assert.AreEqual(instance.Provider.EstablishmentNumber, "3614");
            Assert.AreEqual(instance.Provider.LACode, "201");
            Assert.AreEqual(instance.Provider.DateOpened.Date, DateTime.Now.Date);
            Assert.AreEqual(instance.Provider.CrmAccountId, "99999999999");
            Assert.AreEqual(instance.Provider.Status, "Active");
            Assert.AreEqual(instance.Provider.PhaseOfEducation, "Primary");
            Assert.AreEqual(instance.Provider.LegalName, "I AM LEGAL");
            Assert.AreEqual(instance.Provider.DfeEstablishmentNumber, "77777");
            Assert.AreEqual(instance.Provider.NavVendorNo, "");
            Assert.IsNull(instance.Provider.DateClosed);
        }


        private static Assembly CreateAssembly()
        {
            // Load a pre-generated assembly containing compiled calculations. This was generated by using the system and copying the base64 encoded assembly from cosmos
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "CalculateFunding.Services.Calculator.Resources.test-assembly.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string assemblyAsString = reader.ReadToEnd();

                return Assembly.Load(Convert.FromBase64String(assemblyAsString));
            }
        }

        private static IEnumerable<ProviderSourceDataset> CreateProviderSourceDatasets()
        {
            IEnumerable<ProviderSourceDataset> providerSourceDatasets = new[]
            {
                new ProviderSourceDataset
                {
                    SpecificationId = "7b948df0-8a6a-42ac-b73f-1f342907c69b",
                    ProviderId = "10079319",
                    DataDefinition = new Models.Reference
                    {
                        Id = "9878dcc1-b292-4d99-981b-f7d3000a5d92",
                        Name = "PE and Sport premium"
                    },
                    DataGranularity = DataGranularity.SingleRowPerProvider,
                    DefinesScope = true,
                    Current = new ProviderSourceDatasetVersion
                    {
                        ProviderSourceDatasetId = "7b948df0-8a6a-42ac-b73f-1f342907c69b_9878dcc1-b292-4d99-981b-f7d3000a5d92_10079319",
                        Dataset = new Models.VersionReference("d7264c81-a00e-4322-b83e-f2224cd0ea1f", "AB PE DS 2109002", 2),
                        ProviderId = "10079319",
                        Rows = GetData()
                    },
                    DatasetRelationshipSummary = new Models.Reference
                    {
                        Id = "9878dcc1-b292-4d99-981b-f7d3000a5d92",
                        Name = "PEAndSportPremiumDataset"
                    },
                    DataRelationship = new Models.Reference
                    {
                        Id = "9878dcc1-b292-4d99-981b-f7d3000a5d92",
                        Name = "AB PE 2109001"
                    }
                }
            };

            return providerSourceDatasets;
        }

        private static ProviderSummary CreateProviderSummary()
        {
            return new ProviderSummary
            {
                Id = "10079319",
                Name = "prov name",
                ProviderType = "Voluntary aided school",
                ProviderSubType = "Not Applicable",
                Authority = "authority",
                UKPRN = "10079319",
                UPIN = "12345",
                URN = "100000",
                EstablishmentNumber = "3614",
                LACode = "201",
                DateOpened = DateTime.Now,
                CrmAccountId = "99999999999",
                Status = "Active",
                PhaseOfEducation = "Primary",
                LegalName = "I AM LEGAL",
                DfeEstablishmentNumber = "77777"
            };
        }

        private static List<Dictionary<string, object>> GetData()
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
              {"URN", "100000"},
              {"Local Authority", "201"},
              {"Establishment Number", "3614"},
              {"LAEstab", "2013614"},
              {"School type", "Voluntary aided school"},
              {"Academy Type", "Not Applicable"},
              {"School Name", "Sir John Cass's Foundation Primary School"},
              {"Phase of Education", "Primary"},
              {"Part-Time number of pupils in year group 1 - sole registrations", 10},
              {"Part-Time number of pupils in year group 2 - sole registrations", 0},
              {"Part-Time number of pupils in year group 3 - sole registrations", 0},
              {"Part-Time number of pupils in year group 4 - sole registrations", 11},
              {"Part-Time number of pupils in year group 5 - sole registrations", 0},
              {"Part-Time number of pupils in year group 6 - sole registrations", 0},
              {"Part-Time number of pupils in year group - Not Followed - age 5 to 10 - sole registrations", 0},
              {"Full-Time number of pupils in year group 1 - sole registrations", 59},
              {"Full-Time number of pupils in year group 2 - sole registrations", 30},
              {"Full-Time number of pupils in year group 3 - sole registrations", 31},
              {"Full-Time number of pupils in year group 4 - sole registrations", 30},
              {"Full-Time number of pupils in year group 5 - sole registrations", 30},
              {"Full-Time number of pupils in year group 6 - sole registrations", 29},
              {"Full-Time number of pupils in year group - Not Followed - age 5 to 10 - sole registrations", 0},
              {"Part-Time number of pupils in year group 1 - dual registrations", 0},
              {"Part-Time number of pupils in year group 2 (dual registrations)", 0},
              {"Part-Time number of pupils in year group 3 - dual registrations", 0},
              {"Part-Time number of pupils in year group 4 - dual registrations", 0},
              {"Part-Time number of pupils in year group 5 - dual registrations", 0},
              {"Part-Time number of pupils in year group 6 - dual registrations", 0},
              {"Part-Time number of pupils in year group - Not Followed - age 5 to 10 - dual registrations", 0},
              {"Full-Time number of pupils in year group 1 - dual registrations", 0},
              {"Full-Time number of pupils in year group 2 - dual registrations", 0},
              {"Full-Time number of pupils in year group 3 - dual registrations", 0},
              {"Full-Time number of pupils in year group 4 - dual registrations", 0},
              {"Full-Time number of pupils in year group 5 - dual registrations", 0},
              {"Full-Time number of pupils in year group 6 - dual registrations", 0},
              {"Full-Time number of pupils in year group - Not Followed - age 5 to 10 - dual registrations", 0},
              {"Part-Time pupils aged 5 - Hospital schools}, sole registrations", 0},
              {"Part-Time pupils aged 6 - Hospital schools}, sole registrations", 0},
              {"Part-Time pupils aged 7 - Hospital schools}, sole registrations", 0},
              {"Part-Time pupils aged 8 - Hospital schools}, sole registrations", 0},
              {"Part-Time pupils aged 9 - Hospital schools}, sole registrations", 0},
              {"Part-Time pupils aged 10 - Hospital schools}, sole registrations", 0},
              {"Full-Time pupils aged 5 - Hospital schools}, sole registrations", 0},
              {"Full-Time pupils aged 6 - Hospital schools}, sole registrations", 0},
              {"Full-Time pupils aged 7 - Hospital schools}, sole registrations", 0},
              {"Full-Time pupils aged 8 - Hospital schools}, sole registrations", 0},
              {"Full-Time pupils aged 9 - Hospital schools}, sole registrations", 0},
              {"Full-Time pupils aged 10 - Hospital schools}, sole registrations", 0},
              {"Part-Time pupils aged 5 - Hosptial schools}, dual registrations", 0},
              {"Part-Time pupils aged 6 - Hosptial schools}, dual registrations", 0},
              {"Part-Time pupils aged 7 - Hosptial schools}, dual registrations", 0},
              {"Part-Time pupils aged 8 - Hosptial schools}, dual registrations", 0},
              {"Part-Time pupils aged 9 - Hosptial schools}, dual registrations", 0},
              {"Part-Time pupils aged 10 - Hosptial schools}, dual registrations", 0},
              {"Full-Time pupils aged 5 - Hosptial schools}, dual registrations", 0},
              {"Full-Time pupils aged 6 - Hosptial schools}, dual registrations", 0},
              {"Full-Time pupils aged 7 - Hosptial schools}, dual registrations", 0},
              {"Full-Time pupils aged 8 - Hosptial schools}, dual registrations", 0},
              {"Full-Time pupils aged 9 - Hosptial schools}, dual registrations", 0},
              {"Full-Time pupils aged 10 - Hosptial schools}, dual registrations", 0 }
            };

            return new List<Dictionary<string, object>>
            {
                data
            };
        }
    }
}
